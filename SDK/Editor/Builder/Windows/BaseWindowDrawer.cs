using UnityEngine;

/// <summary>
/// A base class for drawing a window view
/// </summary>
public abstract class BaseWindowDrawer
{
    public Vector2 Size;

    public abstract void Draw(BuildWindowConfig config);

    /// <summary>
    /// Drawn after the scroll view at the window level. Use for footers that should stay
    /// pinned to the bottom of the window (e.g. a primary action button) instead of scrolling.
    /// </summary>
    public virtual void DrawFooter(BuildWindowConfig config)
    {
    }

    public virtual void OnEnabled()
    {

    }
}