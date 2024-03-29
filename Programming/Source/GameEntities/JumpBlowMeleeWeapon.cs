// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using System.Diagnostics;
using Engine;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="JumpBlowMeleeWeapon"/> entity type.
	/// </summary>
	public class JumpBlowMeleeWeaponType : MeleeWeaponType
	{
		[FieldSerialize]
		Vec3 jumpVelocity;

		[FieldSerialize]
		string soundBlowKick;

		//

		[DefaultValue( typeof( Vec3 ), "0 0 0" )]
		public Vec3 JumpVelocity
		{
			get { return jumpVelocity; }
			set { jumpVelocity = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundBlowKick
		{
			get { return soundBlowKick; }
			set { soundBlowKick = value; }
		}

	}

	public class JumpBlowMeleeWeapon : MeleeWeapon
	{
		JumpBlowMeleeWeaponType _type = null; public new JumpBlowMeleeWeaponType Type { get { return _type; } }

		bool firstTick = true;

		[FieldSerialize]
		float lastJumpTime;

		bool collisionEventInitialized;

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( firstTick )
			{
				Character unit = (Character)AttachedMapObjectParent;

				foreach( Body body in unit.PhysicsModel.Bodies )
					body.Collision += new Body.CollisionDelegate( attachedParentBody_Collision );
				collisionEventInitialized = true;

				firstTick = false;
			}

			if( lastJumpTime != 0 )
				lastJumpTime += TickDelta;
		}

		protected override void OnDestroy()
		{
			if( collisionEventInitialized )
			{
				Character unit = (Character)AttachedMapObjectParent;
				if( unit != null )
				{
					foreach( Body body in unit.PhysicsModel.Bodies )
						body.Collision -= new Body.CollisionDelegate( attachedParentBody_Collision );
				}

				collisionEventInitialized = false;
			}

			base.OnDestroy();
		}

		void attachedParentBody_Collision( ref CollisionEvent collisionEvent )
		{
			if( lastJumpTime == 0 )
				return;

			lastJumpTime = 0;

			Dynamic objDynamic = MapSystemWorld.GetMapObjectByBody(
				collisionEvent.OtherShape.Body ) as Dynamic;

			if( objDynamic == null )
				return;

			Character unit = (Character)AttachedMapObjectParent;
			if( unit == null || unit.Intellect == null )
				return;

			//Not kick allies
			Unit objUnit = objDynamic.GetIntellectedRootUnit();
			if( objUnit == null )
				return;
			if( objUnit.Intellect.Faction == unit.Intellect.Faction )
				return;

			objUnit.DoDamage( unit, unit.Position, collisionEvent.OtherShape,
				Type.NormalMode.Damage, true );

			SoundPlay3D( Type.SoundBlowKick, .5f, false );
		}

		protected override void Blow()
		{
			Character unit = (Character)AttachedMapObjectParent;

			if( !unit.IsOnGround() )
				return;

			//jump
			lastJumpTime = .001f;

			unit.TryJump();
			unit.MainBody.Position += new Vec3( 0, 0, .1f );//for no collision event on ground
			unit.MainBody.AngularVelocity = Vec3.Zero;
			unit.MainBody.LinearVelocity = unit.Rotation * Type.JumpVelocity;

			SoundPlay3D( Type.NormalMode.SoundFire, .5f, true );
		}
	}
}
