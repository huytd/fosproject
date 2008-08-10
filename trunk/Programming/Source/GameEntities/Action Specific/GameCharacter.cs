// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="GameCharacter"/> entity type.
	/// </summary>
	public class GameCharacterType : CharacterType
	{
		[FieldSerialize]
		[DefaultValue( typeof( Range ), "0 0" )]
		Range optimalAttackDistanceRange;

		[FieldSerialize]
		[DefaultValue( "idle" )]
		string idleAnimationName = "idle";

		[FieldSerialize]
		[DefaultValue( "walk" )]
		string walkAnimationName = "walk";

		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float walkAnimationVelocityMultiplier = 1;

		[FieldSerialize]
		[DefaultValue( "jump" )]
		string jumpAnimationName = "jump";

		//

		[DefaultValue( typeof( Range ), "0 0" )]
		public Range OptimalAttackDistanceRange
		{
			get { return optimalAttackDistanceRange; }
			set { optimalAttackDistanceRange = value; }
		}

		[DefaultValue( "idle" )]
		public string IdleAnimationName
		{
			get { return idleAnimationName; }
			set { idleAnimationName = value; }
		}

		[DefaultValue( "walk" )]
		public string WalkAnimationName
		{
			get { return walkAnimationName; }
			set { walkAnimationName = value; }
		}

		[DefaultValue( 1.0f )]
		public float WalkAnimationVelocityMultiplier
		{
			get { return walkAnimationVelocityMultiplier; }
			set { walkAnimationVelocityMultiplier = value; }
		}

		[DefaultValue( "jump" )]
		public string JumpAnimationName
		{
			get { return jumpAnimationName; }
			set { jumpAnimationName = value; }
		}
	}

	public class GameCharacter : Character
	{
		Vec3 mainBodyVelocity;

		//

		GameCharacterType _type = null; public new GameCharacterType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			//we call this before OnTick for saving old value of MainBody.LinearVelocity
			mainBodyVelocity = GetGroundRelativeVelocity();

			base.OnTick();
		}

		protected override void OnUpdateAnimation( ref string animationName,
			ref float animationVelocity, ref bool animationLoop, ref bool allowRandomAnimationNumber )
		{
			base.OnUpdateAnimation( ref animationName, ref animationVelocity, ref animationLoop,
				ref allowRandomAnimationNumber );

			//walk animation
			if( IsOnGround() && mainBodyVelocity.ToVec2().LengthSqr() > .3f )
			{
				animationName = Type.WalkAnimationName;
				animationVelocity = ( Rotation.GetInverse() * mainBodyVelocity ).X *
					Type.WalkAnimationVelocityMultiplier;
				animationLoop = true;
				allowRandomAnimationNumber = true;
				return;
			}

			//idle animation
			{
				animationName = Type.IdleAnimationName;
				animationVelocity = 1;
				animationLoop = true;
				allowRandomAnimationNumber = true;
				return;
			}
		}

		protected override void OnJump()
		{
			base.OnJump();

			//jump animation
			if( !string.IsNullOrEmpty( Type.JumpAnimationName ) )
				SetForceAnimation( Type.JumpAnimationName, true );
		}

	}
}
