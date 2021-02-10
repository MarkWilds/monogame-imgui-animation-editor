namespace Editor.Model.Interpolators
{
    public interface IInterpolator
    {
        object Interpolate(float gradient, object first, object second);
    }
}