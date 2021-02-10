using System;
using Editor.Model.Interpolators;

namespace Editor.Model.Interpolators
{
    public class DelegatedInterpolator<T> : IInterpolator
    {
        private Func<float, T, T, T> _implementation;

        public DelegatedInterpolator(Func<float, T, T, T> impl)
        {
            _implementation = impl;
        }
        
        public object Interpolate(float gradient, object first, object second)
        {
            return _implementation(gradient, (T)first, (T)second);
        }
    }
}