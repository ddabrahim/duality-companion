﻿using System;
using Duality.Components;
using Duality.Editor;

namespace Duality.Plugins.Companion.Components
{
	[EditorHintCategory (ResNames.ComponentsEditorCategory)]
	[RequiredComponent (typeof(Camera))]
	public class CameraMan : Component, ICmpUpdatable, ICmpInitializable
	{
		/// <summary>
		/// Takes a time, defined as a float value, and returns a Vector2 which represents
		/// the camera displacement.
		/// To avoid side effects with the other parameters, the resulting Vector's components
		/// should be in the range [-1, 1].
		/// </summary>
		public delegate Vector2 ShakeGenerator (float time);

		/// <summary>
		/// The type of shaking
		/// </summary>
		public enum ShakeDirection
		{
			/// <summary>
			/// Left-Right - implemented as a cosine function.
			/// </summary>
			Horizontal,

			/// <summary>
			/// Up-Down - implemented as a cosine function.
			/// </summary>
			Vertical,

			/// <summary>
			/// Spiral-like - implemented as a cosine function on the X and a sine on the Y.
			/// </summary>
			Circular
		}

		[DontSerialize] private ShakeGenerator shakeGenerator = null;
		[DontSerialize] private float          shakeDamping	  = 0;
		[DontSerialize] private uint           shakeSpeed     = 0;
		[DontSerialize] private float          shakeStrength  = 0;
		[DontSerialize]	private Vector3		   lastMovement   = Vector3.Zero;
		[DontSerialize]	private Transform      transform      = null;

		/// <summary>
		/// The position that the Camera should be following
		/// </summary>
		public Transform Target { get; set; }

		/// <summary>
		/// The percentage of distance to the Target the Camera will move at each frame
		/// 0: don't follow
		/// 1: follow exactly
		/// </summary>
		[EditorHintRange(0, .9f)]
		public float TrailingDistance { get; set; }

		/// <summary>
		/// If set to true, the Camera will always be aligned with the Target's angle
		/// </summary>
		public bool AlignWithTargetDirection { get; set; }

		/// <summary>
		/// It indicates by how much the Camera will zoom out (decrease its own Z) for each unit of speed it is moving at.
		/// If set to a negative value, the Camera will actually zoom in.
		/// </summary>
		public float LeanBackRatio { get; set; }

		public CameraMan()
		{
			this.Target = null;
			this.TrailingDistance = .5f;
			this.AlignWithTargetDirection = false;
			this.LeanBackRatio = 0;
		}

		public void OnUpdate()
		{
			this.transform.MoveBy(-this.lastMovement);

			// Follow target
			if (this.Target != null)
			{
				Vector2 distance = Target.Pos.Xy - this.transform.Pos.Xy;
				this.transform.MoveBy(distance * this.TrailingDistance * Time.TimeMult);

				if (this.AlignWithTargetDirection) this.transform.TurnTo(this.Target.Angle);
			}

			// Add shake effect
			if (this.shakeGenerator != null)
			{
				this.lastMovement.Xy = this.shakeGenerator((float)Time.GameTimer.TotalSeconds * this.shakeSpeed) * this.shakeStrength;
				this.shakeStrength -= this.shakeDamping * Time.SPFMult * Time.TimeMult;

				if (this.shakeStrength <= 1f)
				{
					this.shakeGenerator = null;
					this.lastMovement.Xy = Vector2.Zero;
				}
			}

			this.lastMovement.Z = -(this.transform.Vel.Xy.Length * this.LeanBackRatio);
			this.transform.MoveBy(this.lastMovement);
		}

		/// <summary>
		/// Adds a shake to the camera. If the previous shaking is not complete, the remaining strength
		/// will combine with the new one.
		/// </summary>
		/// <param name="direction">The direction of the shake.</param>
		/// <param name="strength">
		/// The strength of the shake, or how many pixels it will move at most in one direction (assuming
		/// the shake function stays in its default range of [-1, 1]). Defaults to 10.
		/// </param>
		/// <param name="time">How long, in seconds, the shake should last. Defaults to .5.</param>
		/// <param name="speed">The speed of the shake. The lower, the less the camera will shake. Defaults to 100.</param>
		public void Shake(ShakeDirection direction = ShakeDirection.Horizontal, uint strength = 10, float time = .5f, uint speed = 100)
		{
			switch (direction)
			{
				case ShakeDirection.Horizontal:
					Shake(t => new Vector2(MathF.Cos(t), 0), strength, time, speed);
					break;

				case ShakeDirection.Vertical:
					Shake(t => new Vector2(0, MathF.Cos(t)), strength, time, speed);
					break;

				case ShakeDirection.Circular:
					Shake(t => new Vector2(MathF.Sin(t), MathF.Cos(t)), strength, time, speed);
					break;

				default:
					throw new ArgumentOutOfRangeException("Unexpected case");
			}
		}

		/// <summary>
		/// Adds a shake to the camera. If the previous shaking is not complete, the remaining strength
		/// will combine with the new one.
		/// This requires a custom implementation of the generator function.
		/// </summary>
		/// <param name="generator">The function that generates displacement based on time.</param>
		/// <param name="strength">
		/// The strength of the shake, or how many pixels it will move at most in one direction (assuming
		/// the shake function stays in its default range of [-1, 1]).
		/// </param>
		/// <param name="time">How long, in seconds, the shake should last.</param>
		/// <param name="speed">The speed of the shake. The lower, the less the camera will shake.</param>
		public void Shake(ShakeGenerator generator, uint strength, float time, uint speed)
		{
			if (time < 0)
			{
				throw new ArgumentException("Time must be greater than 0");
			}

			this.shakeGenerator = generator;
			this.shakeStrength += strength;
			this.shakeDamping = strength / time;
			this.shakeSpeed = speed;
		}

		public void OnInit(Component.InitContext context)
		{
			this.transform = this.GameObj.Transform;
		}

		public void OnShutdown(Component.ShutdownContext context)
		{
			// nothing to do here
		}
	}
}