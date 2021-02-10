using System;
using System.Collections.Generic;
using Editor.Model.Interpolators;

namespace Editor.Model
{
    public class Animator
    {
        private readonly Dictionary<Type, IInterpolator> _interpolators;
        private readonly Dictionary<string, HashSet<int>> _groups;
        private readonly Dictionary<int, AnimationTrack> _tracks;

        public event Action OnKeyframeChanged;

        private int _currentKeyframe;
        private int _framesPerSecond = 24;
        private float _frameTime = 1.0f / 24f;
        private float _frameTimer = 0f;
        
        private bool _isPlayingBackward;
        private bool _isPlayingForward;
        private bool _isLooping;

        public int CurrentKeyframe
        {
            get => _currentKeyframe;
            set
            {
                if (value != _currentKeyframe)
                {
                    _currentKeyframe = value;
                    OnKeyframeChanged?.Invoke();
                }
            }
        }

        public int FPS
        {
            get => _framesPerSecond;
            set
            {
                _framesPerSecond = value;
                _frameTime = 1.0f / _framesPerSecond;
            }
        }

        public bool Looping => _isLooping;

        public bool Playing => _isPlayingBackward || _isPlayingForward;

        public bool PlayingBackward => _isPlayingBackward;
        
        public bool PlayingForward => _isPlayingForward;

        public Animator()
        {
            _groups = new Dictionary<string, HashSet<int>>(1024);
            _tracks = new Dictionary<int, AnimationTrack>(1024);
            _interpolators = new Dictionary<Type, IInterpolator>(8);
        }

        public void AddInterpolator<T>(Func<float, T, T, T> interpolator)
        {
            var type = typeof(T);
            if (_interpolators.ContainsKey(type))
                throw new Exception($"Interpolator for type {type.Name} already exists");

            _interpolators[type] = new DelegatedInterpolator<T>(interpolator);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _groups.Keys.GetEnumerator();
        }

        public IEnumerable<int> EnumerateGroupTrackIds(string groupName)
        {
            return _groups[groupName];
        }

        public AnimationTrack GetTrack(int trackId)
        {
            return _tracks[trackId];
        }

        public void PlayBackward()
        {
            if (_isPlayingForward)
                _isPlayingForward = false;

            _isPlayingBackward = !_isPlayingBackward;
        }

        public void PlayForward()
        {
            if (_isPlayingBackward)
                _isPlayingBackward = false;

            _isPlayingForward = !_isPlayingForward;
            _frameTime = 0.0f;
        }

        public void Stop()
        {
            _frameTime = 0.0f;
            _isPlayingForward = _isPlayingBackward = false;
        }

        public void ToggleLooping()
        {
            _isLooping = !_isLooping;
        }

        public int GetFirstFrame()
        {
            int firstFrame = int.MaxValue;
            foreach (var track in _tracks.Values)
            {
                if (track.HasKeyframes() && track[0].Frame < firstFrame)
                    firstFrame = track[0].Frame;
            }

            return firstFrame == int.MaxValue ? 0 : firstFrame;
        }

        public int GetLastFrame()
        {
            int lastFrame = int.MinValue;
            foreach (var track in _tracks.Values)
            {
                if (track.HasKeyframes())
                {
                    var lastIndex = track.Count - 1;
                    if (track[lastIndex].Frame > lastFrame)
                        lastFrame = track[lastIndex].Frame;    
                }
            }

            return lastFrame == int.MinValue ? 0 : lastFrame; 
        }

        public int GetPreviousFrame(int? frame = null)
        {
            var f = frame ?? _currentKeyframe;
            var previousFrame = GetFirstFrame();
            foreach (var track in _tracks.Values)
            {
                if(!track.HasKeyframes())
                    continue;
                
                var index = track.GetBestIndex(f) - 1;
                if (index < 0)
                    index = 0;
                else if (index >= track.Count)
                    index = track.Count - 1;
                
                var kf = track[index];

                if (kf.Frame > previousFrame)
                    previousFrame = kf.Frame;
            }

            return f < previousFrame ? f : previousFrame;
        }
        
        public int GetNexFrame(int? frame = null)
        {
            var f = frame ?? _currentKeyframe;
            var nextFrame = GetLastFrame();
            foreach (var track in _tracks.Values)
            {
                if(!track.HasKeyframes())
                    continue;
                
                var index = track.GetExactIndex(f);
                if (index < 0)
                    index = ~index;
                else
                    index++;
                
                if (index < 0)
                    index = 0;
                else if (index >= track.Count)
                    index = track.Count - 1;
                
                var kf = track[index];
                
                if(kf.Frame == _currentKeyframe)
                    continue;

                if (kf.Frame < nextFrame)
                    nextFrame = kf.Frame;
            }

            return f > nextFrame ? f : nextFrame;
        }

        public void Update(float deltaTime)
        {
            if (!Playing)
                return;

            _frameTimer += deltaTime;

            if (!(_frameTimer >= _frameTime))
                return;

            _frameTimer -= _frameTime;

            var lastFrame = GetLastFrame();
            var firstFrame = GetFirstFrame();

            if (_isPlayingBackward)
                CurrentKeyframe--;
            else if (_isPlayingForward)
                CurrentKeyframe++;

            if (_isLooping && HasKeyframes())
            {
                if (CurrentKeyframe > lastFrame)
                    CurrentKeyframe = firstFrame;
                else if (CurrentKeyframe < firstFrame)
                    CurrentKeyframe = lastFrame;
            }
        }

        public bool Interpolate(int trackId, out object value)
        {
            value = null;
            if (!_tracks.ContainsKey(trackId))
                return false;

            var track = _tracks[trackId];
            if (track.Count <= 0)
                return false;

            var interpolator = _interpolators[track.Type];
            var keyFrameIndex = track.GetExactIndex(_currentKeyframe);

            // in between 2 frames
            if (keyFrameIndex >= 0)
            {
                if (keyFrameIndex == track.Count - 1 || keyFrameIndex == 0 && track.Count == 1)
                    value = track[keyFrameIndex].Value;
                else
                {
                    var firstKf = track[keyFrameIndex];
                    var secondKf = track[keyFrameIndex + 1];
                    var fraction = (_currentKeyframe - firstKf.Frame) / (float) (secondKf.Frame - firstKf.Frame);

                    value = interpolator.Interpolate(1 - fraction, firstKf.Value, secondKf.Value);
                }
            }
            else // before or after frames
            {
                var newIndex = ~keyFrameIndex;
                if (newIndex == track.Count)
                    value = track[newIndex - 1].Value;
                else if (newIndex == 0)
                    value = track[newIndex].Value;
                else
                {
                    var firstKf = track[newIndex - 1];
                    var secondKf = track[newIndex];
                    var fraction = (_currentKeyframe - firstKf.Frame) / (float) (secondKf.Frame - firstKf.Frame);

                    value = interpolator.Interpolate(1 - fraction, firstKf.Value, secondKf.Value);
                }
            }

            return true;
        }

        public void AddTrack(string groupName, AnimationTrack track)
        {
            var trackId = GetTrackKey(groupName, track.Id);
            _tracks[trackId] = track;

            if (!_groups.ContainsKey(groupName))
                _groups[groupName] = new HashSet<int> {trackId};
            else
                _groups[groupName].Add(trackId);
        }

        /// <summary>
        /// Returns the track id
        /// </summary>
        public int CreateTrack(Type type, string groupName, string trackName)
        {
            var trackId = GetTrackKey(groupName, trackName);
            if (!_tracks.ContainsKey(trackId))
                _tracks[trackId] = new AnimationTrack(type, trackName);

            if (!_groups.ContainsKey(groupName))
                _groups[groupName] = new HashSet<int> {trackId};
            else
                _groups[groupName].Add(trackId);

            return trackId;
        }

        public void InsertKeyframe(int trackId, object value, int? keyframe = null)
        {
            var kf = keyframe ?? _currentKeyframe;
            var track = _tracks[trackId];

            var index = track.GetBestIndex(kf);
            if (track.HasKeyframeAtFrame(kf))
                track[index].Value = value;
            else
                track.Insert(index, new Keyframe(kf, value));
        }

        public bool HasKeyframes()
        {
            bool hasKeyframes = false;
            foreach (var groupsKey in _groups.Keys)
            {
                hasKeyframes = hasKeyframes || GroupHasKeyframes(groupsKey);
            }

            return hasKeyframes;
        }

        public bool GroupHasKeyframes(string groupName)
        {
            var hasGroup = _groups.ContainsKey(groupName);
            if (!hasGroup)
                return false;

            var hasKeyframes = false;
            foreach (var trackId in _groups[groupName])
            {
                var track = _tracks[trackId];
                hasKeyframes = hasKeyframes || track.HasKeyframes();
            }

            return hasKeyframes;
        }

        public bool GroupHasKeyframeAtFrame(string groupName, int frame)
        {
            var hasGroup = _groups.ContainsKey(groupName);
            if (!hasGroup)
                return false;
            
            foreach (var trackId in _groups[groupName])
            {
                var track = _tracks[trackId];
                if (track.HasKeyframeAtFrame(frame))
                    return true;
            }

            return false;
        }

        public int GetTrackKey(string groupName, string trackName)
        {
            return $"{groupName}_{trackName}".GetHashCode();
        }

        public bool ChangeGroupName(string oldName, string newName)
        {
            if (_groups.ContainsKey(oldName))
            {
                var trackIds = _groups[oldName];
                _groups.Remove(oldName);
                _groups[newName] = trackIds;

                return true;
            }

            return false;
        }

        public void ChangeTrackId(string groupName, string trackName, int oldId)
        {
            if (_groups.ContainsKey(groupName) && _tracks.ContainsKey(oldId))
            {
                var newId = GetTrackKey(groupName, trackName);
                var trackIds = _groups[groupName];
                trackIds.Remove(oldId);
                trackIds.Add(newId);
                
                var track = _tracks[oldId];
                track.Id = trackName;
                _tracks.Remove(oldId);
                _tracks[newId] = track;
            }
        }
    }
}