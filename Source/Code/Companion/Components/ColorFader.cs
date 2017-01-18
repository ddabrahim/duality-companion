﻿using System;
using Duality.Drawing;
using Duality.Editor;
using Duality.Resources;

namespace Duality.Plugins.Companion.Components
{
    public class ColorFader : OverlayRenderer, ICmpUpdatable
    {
        [EditorHintFlags(MemberFlags.Invisible)]
        public EventHandler Faded
        {
            get { return faded; }
            set { faded = value; }
        }

        [DontSerialize]
        private EventHandler faded;

        [DontSerialize]
        private readonly ColorTween colorTween = new ColorTween();

        public void FadeIn(float duration, ColorRgba color, Easing easing)
        {
            if (colorTween.State != TweenState.Running)
            {
                colorTween.Start(color, new ColorRgba(0, 0, 0, 0f), duration, easing);
            }
        }

        public void FadeOut(float duration, ColorRgba color, Easing easing)
        {
            if (colorTween.State != TweenState.Running)
            {
                colorTween.Start(new ColorRgba(0, 0, 0, 0f), color, duration, easing);
            }
        }

        public void OnUpdate()
        {
            if (colorTween.State == TweenState.Running)
                colorTween.Update(Time.LastDelta);

            if (colorTween.State == TweenState.Stopped)
            {
                faded?.Invoke(this, EventArgs.Empty);
                colorTween.Stop(StopBehavior.ForceComplete);
            }
        }

        public override void Draw(IDrawDevice device)
        {
            base.Draw(device);

            if (colorTween.State == TweenState.Running)
            {
                var canvas = new Canvas(device);
                canvas.State.SetMaterial(new BatchInfo(DrawTechnique.Alpha, colorTween.CurrentValue));
                canvas.FillRect(0, 0, DualityApp.TargetResolution.X, DualityApp.TargetResolution.Y);
            }
        }
    }
}