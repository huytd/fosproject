// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.MathEx;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="MeleeWeapon"/> entity type.
	/// </summary>
	public class MeleeWeaponType : WeaponType
	{
		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class MeleeWeaponMode : WeaponMode
		{
			internal MeleeWeaponType owner;

			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float damage;

			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float impulse;

			[FieldSerialize]
			[DefaultValue( 0.5f )]
			float kickCheckRadius = .5f;

			[FieldSerialize]
			string soundKick;

			//

			[DefaultValue( 0.0f )]
			public float Damage
			{
				get { return damage; }
				set { damage = value; }
			}

			[DefaultValue( 0.0f )]
			public float Impulse
			{
				get { return impulse; }
				set { impulse = value; }
			}

			[DefaultValue( 0.5f )]
			public float KickCheckRadius
			{
				get { return kickCheckRadius; }
				set { kickCheckRadius = value; }
			}

			[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
			public string SoundKick
			{
				get { return soundKick; }
				set { soundKick = value; }
			}

			public override string ToString()
			{
				return "MeleeWeapon";
			}

			[Browsable( false )]
			public override bool IsInitialized
			{
				get { return true; }
			}

		}

		[FieldSerialize]
		MeleeWeaponMode normalMode = new MeleeWeaponMode();
		[FieldSerialize]
		MeleeWeaponMode alternativeMode = new MeleeWeaponMode();

		public MeleeWeaponType()
		{
			weaponNormalMode = normalMode;
			weaponAlternativeMode = alternativeMode;
			normalMode.owner = this;
			alternativeMode.owner = this;
		}

		public MeleeWeaponMode NormalMode
		{
			get { return normalMode; }
		}

		public MeleeWeaponMode AlternativeMode
		{
			get { return alternativeMode; }
		}
	}

	public class MeleeWeapon : Weapon
	{
		float readyTimeRemaining;

		//for FireTimes
		float currentFireTime;
		MeleeWeaponType.MeleeWeaponMode currentFireTypeMode;
		int fireTimesExecuted;

		//

		MeleeWeaponType _type = null; public new MeleeWeaponType Type { get { return _type; } }

		[Browsable( false )]
		public override bool Ready
		{
			get { return readyTimeRemaining == 0; }
		}

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

			//fireTimes
			if( currentFireTypeMode != null )
			{
				currentFireTime += TickDelta;
				int fireTimesCount = currentFireTypeMode.FireTimes.Count;
				if( fireTimesExecuted < fireTimesCount )
				{
					again:
					if( fireTimesExecuted < fireTimesCount )
					{
						float nextTime = currentFireTypeMode.FireTimes[ fireTimesExecuted ];
						if( currentFireTime >= nextTime )
						{
							fireTimesExecuted++;
							Blow();
							goto again;
						}
					}
				}
				else
					currentFireTypeMode = null;
			}
			
			if( readyTimeRemaining != 0 )
			{
				float coef = 1.0f;

				Unit unit = GetIntellectedRootUnit();
				if( unit != null && unit.FastAttackInfluence != null )
					coef *= unit.FastAttackInfluence.Type.Coefficient;

				readyTimeRemaining -= TickDelta * coef;
				if( readyTimeRemaining < 0 )
					readyTimeRemaining = 0;
			}
		}


		public override bool TryFire( bool alternative )
		{
			if( !Ready )
				return false;

			MeleeWeaponType.MeleeWeaponMode typeMode = alternative ? 
				Type.AlternativeMode : Type.NormalMode;

			if( typeMode.Damage == 0 && typeMode.Impulse == 0 )
				return false;

			Fire( alternative );
			return true;
		}

		protected virtual void Fire( bool alternative )
		{
			DoPreFireEvent( alternative );

			MeleeWeaponType.MeleeWeaponMode typeMode = alternative ? 
				Type.AlternativeMode : Type.NormalMode;

			if( typeMode.FireTimes.Count == 0 )
				return;

			readyTimeRemaining = typeMode.BetweenFireTime;
			currentFireTypeMode = typeMode;
			currentFireTime = 0;
			fireTimesExecuted = 0;

			//sound
			SoundPlay3D( currentFireTypeMode.SoundFire, .5f, true );

			//animation
			SetForceAnimationState( typeMode.FireAnimationName );

			//parent unit animation
			if( !string.IsNullOrEmpty( typeMode.FireUnitAnimationName ) )
			{
				Unit parentUnit = GetParentUnit();
				if( parentUnit != null )
					parentUnit.SetForceAnimationState( typeMode.FireUnitAnimationName );
			}
		}

		public override Quat GetFireRotation( bool alternative )
		{
			MeleeWeaponType.WeaponMode typeMode = alternative ? Type.AlternativeMode : Type.NormalMode;
			return GetFireRotation( typeMode );
		}

		public override Vec3 GetFirePosition( bool alternative )
		{
			MeleeWeaponType.WeaponMode typeMode = alternative ? Type.AlternativeMode : Type.NormalMode;
			return GetFirePosition( typeMode );
		}

		protected virtual void Blow()
		{
			if( currentFireTypeMode == null )
				return;

			Unit unit = GetIntellectedRootUnit();

			Sphere kickSphere = new Sphere( Position, currentFireTypeMode.KickCheckRadius );

			bool playSound = false;

			Sphere volume = new Sphere( Position, currentFireTypeMode.KickCheckRadius );
			Body[] volumeResult = PhysicsWorld.Instance.VolumeCast( volume,
				(int)ContactGroup.CastOnlyContact );
			foreach( Body body in volumeResult )
			{
				//no kick
				if( body.Shapes[ 0 ].ContactGroup == (int)ContactGroup.NoContact )
					continue;

				Dynamic dynamic = MapSystemWorld.GetMapObjectByBody( body ) as Dynamic;

				if( dynamic != null )
				{
					//not kick allies
					Unit objUnit = dynamic.GetIntellectedRootUnit();
					if( objUnit != null && objUnit.Intellect != null && unit.Intellect != null &&
						objUnit.Intellect.Faction == unit.Intellect.Faction )
						continue;

					//inpulse
					float impulse = currentFireTypeMode.Impulse;
					if( impulse != 0 && dynamic.PhysicsModel != null )
					{
						foreach( Body b in dynamic.PhysicsModel.Bodies )
						{
							if( b.Shapes[ 0 ].ContactGroup != (int)ContactGroup.NoContact )
							{
								Vec3 dir = b.Position - unit.Position;
								dir.NormalizeFast();
								body.AddForce( ForceType.GlobalAtGlobalPos, 0, dir * impulse, Position );
							}
						}
					}

					//damage
					float damage = currentFireTypeMode.Damage;
					if( damage != 0 )
						dynamic.DoDamage( unit, Position, null, damage, false );
				}

				playSound = true;
			}

			if( playSound )
				SoundPlay3D( currentFireTypeMode.SoundKick, .5f, false );
		}
	}
}
