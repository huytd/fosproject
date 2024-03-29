// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;
using Engine.SoundSystem;
using Engine.Utils;
using GameCommon;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Tank"/> entity type.
	/// </summary>
	public class TankType : UnitType
	{
		[FieldSerialize]
		float maxForwardSpeed = 20;

		[FieldSerialize]
		float maxBackwardSpeed = 10;

		[FieldSerialize]
		float driveForwardForce = 300;

		[FieldSerialize]
		float driveBackwardForce = 200;

		[FieldSerialize]
		float brakeForce = 400;

		[FieldSerialize]
		Range gunRotationAngleRange = new Range( -8, 40 );

		[FieldSerialize]
		Range optimalAttackDistanceRange;

		[FieldSerialize]
		List<Gear> gears = new List<Gear>();

		[FieldSerialize]
		Degree towerTurnSpeed = 60;

		[FieldSerialize]
		string soundOn;

		[FieldSerialize]
		string soundOff;

		[FieldSerialize]
		string soundGearUp;

		[FieldSerialize]
		string soundGearDown;

		[FieldSerialize]
		string soundTowerTurn;

		[FieldSerialize]
		[DefaultValue( typeof( Vec2 ), "1 0" )]
		Vec2 tracksAnimationMultiplier = new Vec2( 1, 0 );

		[FieldSerialize]
		float mainGunRecoilForce;

		///////////////////////////////////////////

		public class Gear
		{
			[FieldSerialize]
			int number;

			[FieldSerialize]
			Range speedRange;

			[FieldSerialize]
			string soundMotor;

			[FieldSerialize]
			[DefaultValue( typeof( Range ), "1 1.2" )]
			Range soundMotorPitchRange = new Range( 1, 1.2f );

			//

			[DefaultValue( 0 )]
			public int Number
			{
				get { return number; }
				set { number = value; }
			}

			[DefaultValue( typeof( Range ), "0 0" )]
			public Range SpeedRange
			{
				get { return speedRange; }
				set { speedRange = value; }
			}

			[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
			public string SoundMotor
			{
				get { return soundMotor; }
				set { soundMotor = value; }
			}

			[DefaultValue( typeof( Range ), "1 1.2" )]
			public Range SoundMotorPitchRange
			{
				get { return soundMotorPitchRange; }
				set { soundMotorPitchRange = value; }
			}

			public override string ToString()
			{
				return string.Format( "Gear {0}", number );
			}
		}

		///////////////////////////////////////////

		[DefaultValue( 20.0f )]
		public float MaxForwardSpeed
		{
			get { return maxForwardSpeed; }
			set { maxForwardSpeed = value; }
		}

		[DefaultValue( 10.0f )]
		public float MaxBackwardSpeed
		{
			get { return maxBackwardSpeed; }
			set { maxBackwardSpeed = value; }
		}

		[DefaultValue( 300.0f )]
		public float DriveForwardForce
		{
			get { return driveForwardForce; }
			set { driveForwardForce = value; }
		}

		[DefaultValue( 200.0f )]
		public float DriveBackwardForce
		{
			get { return driveBackwardForce; }
			set { driveBackwardForce = value; }
		}

		[DefaultValue( 400.0f )]
		public float BrakeForce
		{
			get { return brakeForce; }
			set { brakeForce = value; }
		}

		[Description( "In degrees." )]
		[DefaultValue( typeof( Range ), "-8 40" )]
		public Range GunRotationAngleRange
		{
			get { return gunRotationAngleRange; }
			set { gunRotationAngleRange = value; }
		}

		[DefaultValue( typeof( Range ), "0 0" )]
		public Range OptimalAttackDistanceRange
		{
			get { return optimalAttackDistanceRange; }
			set { optimalAttackDistanceRange = value; }
		}

		public List<Gear> Gears
		{
			get { return gears; }
		}

		[Description( "Degrees per second." )]
		[DefaultValue( typeof( Degree ), "60" )]
		public Degree TowerTurnSpeed
		{
			get { return towerTurnSpeed; }
			set { towerTurnSpeed = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundOn
		{
			get { return soundOn; }
			set { soundOn = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundOff
		{
			get { return soundOff; }
			set { soundOff = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundGearUp
		{
			get { return soundGearUp; }
			set { soundGearUp = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundGearDown
		{
			get { return soundGearDown; }
			set { soundGearDown = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundTowerTurn
		{
			get { return soundTowerTurn; }
			set { soundTowerTurn = value; }
		}

		[DefaultValue( typeof( Vec2 ), "1 0" )]
		public Vec2 TracksAnimationMultiplier
		{
			get { return tracksAnimationMultiplier; }
			set { tracksAnimationMultiplier = value; }
		}

		[DefaultValue( 0.0f )]
		public float MainGunRecoilForce
		{
			get { return mainGunRecoilForce; }
			set { mainGunRecoilForce = value; }
		}
	}

	////////////////////////////////////////////////////////////////////////////////////////////////

	public class Tank : Unit
	{
		Track leftTrack = new Track();
		Track rightTrack = new Track();
		float tracksPositionYOffset;

		Body chassisBody;
		float chassisSleepTimer;
		Body towerBody;
		Vec3 towerBodyLocalPosition;

		MapObjectAttachedMapObject mainGunAttachedObject;
		Gun mainGun;
		Vec3 mainGunOffsetPosition;

		SphereDir towerLocalDirection;
		SphereDir needTowerLocalDirection;

		//currently gears used only for sounds
		TankType.Gear currentGear;

		bool motorOn;
		string currentMotorSoundName;
		VirtualChannel motorSoundChannel;
		VirtualChannel towerTurnChannel;

		bool firstTick = true;

		//Minefield specific
		float minefieldUpdateTimer;

		///////////////////////////////////////////

		class Track
		{
			public List<MapObjectAttachedHelper> trackHelpers = new List<MapObjectAttachedHelper>();
			public bool onGround = true;

			//animation
			public MeshObject meshObject;
			public Vec2 materialScrollValue;
		}

		///////////////////////////////////////////

		TankType _type = null; public new TankType Type { get { return _type; } }

		public Tank()
		{
			//Minefield specific
			minefieldUpdateTimer = World.Instance.Random.NextFloat();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			AddTimer();

			if( EntitySystemWorld.Instance.WorldSimulationType != WorldSimulationType.Editor )
			{
				if( PhysicsModel == null )
				{
					Log.Error( "Tank: Physics model not exists." );
					return;
				}

				chassisBody = PhysicsModel.GetBody( "chassis" );
				if( chassisBody == null )
				{
					Log.Error( "Tank: \"chassis\" body not exists." );
					return;
				}
				towerBody = PhysicsModel.GetBody( "tower" );

				//chassisBody.Collision += chassisBody_Collision;

				foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
				{
					if( attachedObject.Alias == "leftTrack" )
						leftTrack.trackHelpers.Add( (MapObjectAttachedHelper)attachedObject );
					if( attachedObject.Alias == "rightTrack" )
						rightTrack.trackHelpers.Add( (MapObjectAttachedHelper)attachedObject );
				}

				if( leftTrack.trackHelpers.Count != 0 )
					tracksPositionYOffset = Math.Abs( leftTrack.trackHelpers[ 0 ].PositionOffset.Y );
			}

			//mainGun
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject == null )
					continue;

				mainGun = attachedMapObject.MapObject as Gun;
				if( mainGun != null )
				{
					mainGunAttachedObject = attachedMapObject;
					mainGunOffsetPosition = attachedMapObject.PositionOffset;
					break;
				}
			}

			//towerBodyLocalPosition
			if( towerBody != null )
				towerBodyLocalPosition = PhysicsModel.ModelDeclaration.GetBody( towerBody.Name ).Position;

			//initialize currentGear
			currentGear = Type.Gears.Find( delegate( TankType.Gear gear )
			{
				return gear.Number == 0;
			} );

			//That the body did not fall after loading a map.
			//After loading a map, the physics simulate 5 seconds, that bodies have fallen asleep.
			if( loaded && EntitySystemWorld.Instance.SerializationMode == SerializationModes.Map )
			{
				if( chassisBody != null )
					chassisBody.Static = true;
				if( towerBody != null )
					towerBody.Static = true;
			}

			//replace track materials
			if( EntitySystemWorld.Instance.WorldSimulationType != WorldSimulationType.Editor )
				InitTracksAnimation();
		}

		protected override void OnDestroy()
		{
			if( motorSoundChannel != null )
			{
				motorSoundChannel.Stop();
				motorSoundChannel = null;
			}

			if( towerTurnChannel != null )
			{
				towerTurnChannel.Stop();
				towerTurnChannel = null;
			}

			ShutdownTracksAnimation();

			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			//That the body did not fall after loading a map. 
			//After loading a map, the physics simulate 5 seconds, that bodies have fallen asleep.
			if( firstTick )
			{
				if( chassisBody != null )
					chassisBody.Static = false;
				if( towerBody != null )
					towerBody.Static = false;
			}

			TickChassisGround();
			TickChassis();

			if( Intellect != null )
			{
				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire1 ) )
				{
					if( GunsTryFire( false ) )
						AddMainGunRecoilForce();
				}

				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire2 ) )
					GunsTryFire( true );
			}

			TickCurrentGear();
			TickMotorSound();

			TickTowerTurn();

			//Minefield specific
			TickMinefields();

			firstTick = false;
		}

		void AddMainGunRecoilForce()
		{
			if( mainGun != null && chassisBody != null && Type.MainGunRecoilForce != 0 )
			{
				chassisBody.AddForce( ForceType.GlobalAtGlobalPos, 0,
					mainGun.Rotation * new Vec3( -Type.MainGunRecoilForce, 0, 0 ), mainGun.Position );
			}
		}

		void TickMotorSound()
		{
			bool lastMotorOn = motorOn;
			motorOn = Intellect != null && Intellect.IsActive();

			//sound on, off
			if( motorOn != lastMotorOn )
			{
				if( !firstTick && Life != 0 )
				{
					if( motorOn )
						SoundPlay3D( Type.SoundOn, .7f, true );
					else
						SoundPlay3D( Type.SoundOff, .7f, true );
				}
			}

			string needSoundName = null;
			if( motorOn && currentGear != null )
				needSoundName = currentGear.SoundMotor;

			if( needSoundName != currentMotorSoundName )
			{
				//change motor sound

				if( motorSoundChannel != null )
				{
					motorSoundChannel.Stop();
					motorSoundChannel = null;
				}

				currentMotorSoundName = needSoundName;

				if( !string.IsNullOrEmpty( needSoundName ) )
				{
					Sound sound = SoundWorld.Instance.SoundCreate( needSoundName,
						SoundMode.Mode3D | SoundMode.Loop );

					if( sound != null )
					{
						motorSoundChannel = SoundWorld.Instance.SoundPlay(
							sound, EngineApp.Instance.DefaultSoundChannelGroup, .3f, true );
						motorSoundChannel.Position = Position;
						motorSoundChannel.Pause = false;
					}
				}
			}

			//update motor channel position and pitch
			if( motorSoundChannel != null )
			{
				Range speedRangeAbs = currentGear.SpeedRange;
				if( speedRangeAbs.Minimum < 0 && speedRangeAbs.Maximum < 0 )
					speedRangeAbs = new Range( -speedRangeAbs.Maximum, -speedRangeAbs.Minimum );
				Range pitchRange = currentGear.SoundMotorPitchRange;

				float speedAbs = Math.Abs( GetTracksSpeed() );

				float speedCoef = 0;
				if( speedRangeAbs.Size() != 0 )
					speedCoef = ( speedAbs - speedRangeAbs.Minimum ) / speedRangeAbs.Size();
				MathFunctions.Clamp( ref speedCoef, 0, 1 );

				//update channel
				motorSoundChannel.Pitch = pitchRange.Minimum + speedCoef * pitchRange.Size();
				motorSoundChannel.Position = Position;
			}
		}

		protected override void OnIntellectCommand( Intellect.Command command )
		{
			base.OnIntellectCommand( command );

			if( command.KeyPressed )
			{
				if( command.Key == GameControlKeys.Fire1 )
				{
					if( GunsTryFire( false ) )
						AddMainGunRecoilForce();
				}
				if( command.Key == GameControlKeys.Fire2 )
					GunsTryFire( true );
			}
		}

		bool GunsTryFire( bool alternative )
		{
			bool fire = false;

			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
				if( attachedMapObject == null )
					continue;

				Gun gun = attachedMapObject.MapObject as Gun;

				if( gun != null )
				{
					if( gun.TryFire( alternative ) )
						fire = true;
				}
			}

			return fire;
		}

		void TickChassisGround()
		{
			if( chassisBody == null )
				return;

			if( chassisBody.Sleeping )
				return;

			//!!!!!
			float rayLength = .7f;

			leftTrack.onGround = false;
			rightTrack.onGround = false;

			float mass = 0;
			foreach( Body body in PhysicsModel.Bodies )
				mass += body.Mass;

			int helperCount = leftTrack.trackHelpers.Count + rightTrack.trackHelpers.Count;

			float verticalVelocity =
				( chassisBody.Rotation.GetInverse() * chassisBody.LinearVelocity ).Z;

			for( int side = 0; side < 2; side++ )
			{
				Track track = side == 0 ? leftTrack : rightTrack;

				foreach( MapObjectAttachedHelper trackHelper in track.trackHelpers )
				{
					Vec3 pos;
					Quat rot;
					Vec3 scl;
					trackHelper.GetGlobalTransform( out pos, out rot, out scl );

					Vec3 downDirection = chassisBody.Rotation * new Vec3( 0, 0, -rayLength );

					Vec3 start = pos - downDirection;

					Ray ray = new Ray( start, downDirection );
					RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
						ray, (int)ContactGroup.CastOnlyContact );

					bool collision = false;
					Vec3 collisionPos = Vec3.Zero;

					foreach( RayCastResult result in piercingResult )
					{
						if( Array.IndexOf( PhysicsModel.Bodies, result.Shape.Body ) != -1 )
							continue;
						collision = true;
						collisionPos = result.Position;
						break;
					}

					if( collision )
					{
						track.onGround = true;

						float distance = ( collisionPos - start ).Length();

						if( distance < rayLength )
						{
							float needCoef = ( rayLength - distance ) / rayLength;

							//!!!!!!

							float force = 0;
							//anti gravity
							force += ( -PhysicsWorld.Instance.Gravity.Z * mass ) / (float)helperCount;
							//anti vertical velocity
							force += ( -verticalVelocity * mass ) / (float)helperCount;

							//!!!!
							force *= ( needCoef + .45f );

							chassisBody.AddForce( ForceType.GlobalAtGlobalPos,
								TickDelta, new Vec3( 0, 0, force ), pos );
						}
					}
				}
			}
		}

		void TickChassis()
		{
			bool onGround = leftTrack.onGround || rightTrack.onGround;

			float leftTrackThrottle = 0;
			float rightTrackThrottle = 0;
			if( Intellect != null )
			{
				float forward = Intellect.GetControlKeyStrength( GameControlKeys.Forward );
				leftTrackThrottle += forward;
				rightTrackThrottle += forward;

				float backward = Intellect.GetControlKeyStrength( GameControlKeys.Backward );
				leftTrackThrottle -= backward;
				rightTrackThrottle -= backward;

				float left = Intellect.GetControlKeyStrength( GameControlKeys.Left );
				leftTrackThrottle -= left * 2;
				rightTrackThrottle += left * 2;

				float right = Intellect.GetControlKeyStrength( GameControlKeys.Right );
				leftTrackThrottle += right * 2;
				rightTrackThrottle -= right * 2;

				MathFunctions.Clamp( ref leftTrackThrottle, -1, 1 );
				MathFunctions.Clamp( ref rightTrackThrottle, -1, 1 );
			}

			//return if no throttle and sleeping
			if( chassisBody.Sleeping && rightTrackThrottle == 0 && leftTrackThrottle == 0 )
				return;

			Vec3 localLinearVelocity = chassisBody.LinearVelocity * chassisBody.Rotation.GetInverse();

			//add drive force

			float slopeForwardForceCoeffient;
			float slopeBackwardForceCoeffient;
			float slopeLinearDampingAddition;
			{
				Vec3 dir = chassisBody.Rotation.GetForward();
				Radian slopeAngle = MathFunctions.ATan( dir.Z, dir.ToVec2().Length() );

				Radian maxAngle = MathFunctions.PI / 4;//new Degree(45)

				slopeForwardForceCoeffient = 1;
				if( slopeAngle > maxAngle )
					slopeForwardForceCoeffient = 0;

				slopeBackwardForceCoeffient = 1;
				if( slopeAngle < -maxAngle )
					slopeBackwardForceCoeffient = 0;

				MathFunctions.Clamp( ref slopeForwardForceCoeffient, 0, 1 );
				MathFunctions.Clamp( ref slopeBackwardForceCoeffient, 0, 1 );

				slopeLinearDampingAddition = localLinearVelocity.X > 0 ? slopeAngle : -slopeAngle;
				//slopeLinearDampingAddition *= 1;
				if( slopeLinearDampingAddition < 0 )
					slopeLinearDampingAddition = 0;
			}

			if( leftTrack.onGround )
			{
				if( leftTrackThrottle > 0 && localLinearVelocity.X < Type.MaxForwardSpeed )
				{
					float force = localLinearVelocity.X > 0 ? Type.DriveForwardForce : Type.BrakeForce;
					force *= leftTrackThrottle;
					force *= slopeForwardForceCoeffient;
					chassisBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
						new Vec3( force, 0, 0 ), new Vec3( 0, tracksPositionYOffset, 0 ) );
				}

				if( leftTrackThrottle < 0 && ( -localLinearVelocity.X ) < Type.MaxBackwardSpeed )
				{
					float force = localLinearVelocity.X > 0 ? Type.BrakeForce : Type.DriveBackwardForce;
					force *= leftTrackThrottle;
					force *= slopeBackwardForceCoeffient;
					chassisBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
						new Vec3( force, 0, 0 ), new Vec3( 0, tracksPositionYOffset, 0 ) );
				}
			}

			if( rightTrack.onGround )
			{
				if( rightTrackThrottle > 0 && localLinearVelocity.X < Type.MaxForwardSpeed )
				{
					float force = localLinearVelocity.X > 0 ? Type.DriveForwardForce : Type.BrakeForce;
					force *= rightTrackThrottle;
					force *= slopeForwardForceCoeffient;
					chassisBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
						new Vec3( force, 0, 0 ), new Vec3( 0, -tracksPositionYOffset, 0 ) );
				}

				if( rightTrackThrottle < 0 && ( -localLinearVelocity.X ) < Type.MaxBackwardSpeed )
				{
					float force = localLinearVelocity.X > 0 ? Type.BrakeForce : Type.DriveBackwardForce;
					force *= rightTrackThrottle;
					force *= slopeBackwardForceCoeffient;
					chassisBody.AddForce( ForceType.LocalAtLocalPos, TickDelta,
						new Vec3( force, 0, 0 ), new Vec3( 0, -tracksPositionYOffset, 0 ) );
				}
			}

			//LinearVelocity
			if( onGround && localLinearVelocity != Vec3.Zero )
			{
				Vec3 velocity = localLinearVelocity;
				velocity.Y = 0;
				chassisBody.LinearVelocity = chassisBody.Rotation * velocity;
			}

			bool stop = onGround && leftTrackThrottle == 0 && rightTrackThrottle == 0;

			bool noLinearVelocity = chassisBody.LinearVelocity.Equals( Vec3.Zero, .2f );
			bool noAngularVelocity = chassisBody.AngularVelocity.Equals( Vec3.Zero, .2f );

			//LinearDamping
			float linearDamping;
			if( stop )
				linearDamping = noLinearVelocity ? 5 : 1;
			else
				linearDamping = .15f;
			chassisBody.LinearDamping = linearDamping + slopeLinearDampingAddition;

			//AngularDamping
			if( onGround )
			{
				if( stop && noAngularVelocity )
					chassisBody.AngularDamping = 5;
				else
					chassisBody.AngularDamping = 1;
			}
			else
				chassisBody.AngularDamping = .15f;

			//sleeping
			if( !chassisBody.Sleeping && stop && noLinearVelocity && noAngularVelocity )
			{
				chassisSleepTimer += TickDelta;
				if( chassisSleepTimer > 1 )
					chassisBody.Sleeping = true;
			}
			else
				chassisSleepTimer = 0;
		}

		[Browsable( false )]
		public Gun MainGun
		{
			get { return mainGun; }
		}

		public void SetMomentaryTurnToPosition( Vec3 pos )
		{
			if( towerBody == null )
				return;

			Vec3 direction = pos - towerBody.Position;
			towerLocalDirection = SphereDir.FromVector( Rotation.GetInverse() * direction );
			needTowerLocalDirection = towerLocalDirection;
		}

		public void SetNeedTurnToPosition( Vec3 pos )
		{
			if( towerBody == null )
				return;

			if( Type.TowerTurnSpeed != 0 )
			{
				Vec3 direction = pos - towerBody.Position;
				needTowerLocalDirection = SphereDir.FromVector( Rotation.GetInverse() * direction );
			}
			else
				SetMomentaryTurnToPosition( pos );
		}

		protected override void OnRender( Camera camera )
		{
			//not very true update in the OnRender.
			//it is here because need update after all Ticks and before update attached objects.
			UpdateTowerTransform();

			base.OnRender( camera );
		}

		void UpdateTowerTransform()
		{
			if( towerBody == null || chassisBody == null || mainGunAttachedObject == null )
				return;

			Radian horizontalAngle = towerLocalDirection.Horizontal;
			Radian verticalAngle = towerLocalDirection.Vertical;

			Range gunRotationRange = Type.GunRotationAngleRange * MathFunctions.PI / 180.0f;
			if( verticalAngle < gunRotationRange.Minimum )
				verticalAngle = gunRotationRange.Minimum;
			if( verticalAngle > gunRotationRange.Maximum )
				verticalAngle = gunRotationRange.Maximum;

			//update tower body
			towerBody.Position = GetInterpolatedPosition() +
				GetInterpolatedRotation() * towerBodyLocalPosition;
			towerBody.Rotation = GetInterpolatedRotation() *
				new Angles( 0, 0, -horizontalAngle.InDegrees() ).ToQuat();
			towerBody.Sleeping = true;

			//update gun vertical rotation
			Quat verticalRotation = new Angles( 0, verticalAngle.InDegrees(), 0 ).ToQuat();
			mainGunAttachedObject.RotationOffset = verticalRotation;
		}

		float GetTracksSpeed()
		{
			if( chassisBody == null )
				return 0;

			Vec3 linearVelocity = chassisBody.LinearVelocity;
			Vec3 angularVelocity = chassisBody.AngularVelocity;

			//optimization
			if( linearVelocity.Equals( Vec3.Zero, .1f ) && angularVelocity.Equals( Vec3.Zero, .1f ) )
				return 0;

			Vec3 localLinearVelocity = linearVelocity * chassisBody.Rotation.GetInverse();

			//not ideal true
			return localLinearVelocity.X + Math.Abs( angularVelocity.Z ) * 2;
		}

		void TickCurrentGear()
		{
			//currently gears used only for sounds

			if( currentGear == null )
				return;

			if( motorOn )
			{
				float speed = GetTracksSpeed();

				TankType.Gear newGear = null;

				if( speed < currentGear.SpeedRange.Minimum || speed > currentGear.SpeedRange.Maximum )
				{
					//find new gear
					newGear = Type.Gears.Find( delegate( TankType.Gear gear )
					{
						return speed >= gear.SpeedRange.Minimum && speed <= gear.SpeedRange.Maximum;
					} );
				}

				if( newGear != null && currentGear != newGear )
				{
					//change gear
					TankType.Gear oldGear = currentGear;
					OnGearChange( oldGear, newGear );
					currentGear = newGear;
				}
			}
			else
			{
				if( currentGear.Number != 0 )
				{
					currentGear = Type.Gears.Find( delegate( TankType.Gear gear )
					{
						return gear.Number == 0;
					} );
				}
			}
		}

		void OnGearChange( TankType.Gear oldGear, TankType.Gear newGear )
		{
			if( !firstTick && Life != 0 )
			{
				bool up = Math.Abs( newGear.Number ) > Math.Abs( oldGear.Number );
				string soundName = up ? Type.SoundGearUp : Type.SoundGearDown;
				SoundPlay3D( soundName, .7f, true );
			}
		}

		void TickTowerTurn()
		{
			//update direction
			if( towerLocalDirection != needTowerLocalDirection )
			{
				Radian turnSpeed = Type.TowerTurnSpeed;

				SphereDir needDirection = needTowerLocalDirection;
				SphereDir direction = towerLocalDirection;

				//update horizontal direction
				float diffHorizontalAngle = needDirection.Horizontal - direction.Horizontal;
				while( diffHorizontalAngle < -MathFunctions.PI )
					diffHorizontalAngle += MathFunctions.PI * 2;
				while( diffHorizontalAngle > MathFunctions.PI )
					diffHorizontalAngle -= MathFunctions.PI * 2;

				if( diffHorizontalAngle > 0 )
				{
					if( direction.Horizontal > needDirection.Horizontal )
						direction.Horizontal -= MathFunctions.PI * 2;
					direction.Horizontal += turnSpeed * TickDelta;
					if( direction.Horizontal > needDirection.Horizontal )
						direction.Horizontal = needDirection.Horizontal;
				}
				else
				{
					if( direction.Horizontal < needDirection.Horizontal )
						direction.Horizontal += MathFunctions.PI * 2;
					direction.Horizontal -= turnSpeed * TickDelta;
					if( direction.Horizontal < needDirection.Horizontal )
						direction.Horizontal = needDirection.Horizontal;
				}

				//update vertical direction
				if( direction.Vertical < needDirection.Vertical )
				{
					direction.Vertical += turnSpeed * TickDelta;
					if( direction.Vertical > needDirection.Vertical )
						direction.Vertical = needDirection.Vertical;
				}
				else
				{
					direction.Vertical -= turnSpeed * TickDelta;
					if( direction.Vertical < needDirection.Vertical )
						direction.Vertical = needDirection.Vertical;
				}

				towerLocalDirection = direction;
				if( towerLocalDirection.Equals( needTowerLocalDirection, .001f ) )
					towerLocalDirection = needTowerLocalDirection;
			}

			//update tower turn sound
			{
				bool needSound = !towerLocalDirection.Equals( needTowerLocalDirection,
					new Degree( 2 ).InRadians() );

				if( needSound )
				{
					if( towerTurnChannel == null && !string.IsNullOrEmpty( Type.SoundTowerTurn ) )
					{
						Sound sound = SoundWorld.Instance.SoundCreate( Type.SoundTowerTurn,
							SoundMode.Mode3D | SoundMode.Loop );

						if( sound != null )
						{
							towerTurnChannel = SoundWorld.Instance.SoundPlay(
								sound, EngineApp.Instance.DefaultSoundChannelGroup, .3f, true );
							towerTurnChannel.Position = Position;
							towerTurnChannel.Pause = false;
						}
					}

					if( towerTurnChannel != null )
						towerTurnChannel.Position = Position;
				}
				else
				{
					if( towerTurnChannel != null )
					{
						towerTurnChannel.Stop();
						towerTurnChannel = null;
					}
				}
			}

		}

		void InitTracksAnimation()
		{
			for( int nTrack = 0; nTrack < 2; nTrack++ )
			{
				Track track = nTrack == 0 ? leftTrack : rightTrack;
				string alias = nTrack == 0 ? "leftTrackMesh" : "rightTrackMesh";

				MapObjectAttachedMesh attachedMesh = GetAttachedObjectByAlias( alias )
					as MapObjectAttachedMesh;
				if( attachedMesh != null )
				{
					track.meshObject = attachedMesh.MeshObject;
					if( track.meshObject != null )
						track.meshObject.AddToRenderQueue += TrackMeshObject_AddToRenderQueue;
				}
			}
		}

		void ShutdownTracksAnimation()
		{
			leftTrack.meshObject = null;
			rightTrack.meshObject = null;
		}

		void TrackMeshObject_AddToRenderQueue( MovableObject sender,
			Camera camera, bool onlyShadowCasters, ref bool allowRender )
		{
			//tracks animation

			if( camera != RendererWorld.Instance.DefaultCamera )
				return;
			if( leftTrack.meshObject == null )
				return;

			//!!!!!need speed for each track
			float speed = GetTracksSpeed();

			Track track = leftTrack.meshObject == sender ? leftTrack : rightTrack;

			Vec2 value = track.materialScrollValue + Type.TracksAnimationMultiplier *
				( speed * RendererWorld.Instance.FrameRenderTimeStep );

			while( value.X < 0 ) value.X++;
			while( value.X >= 1 ) value.X--;
			while( value.Y < 0 ) value.Y++;
			while( value.Y >= 1 ) value.Y--;

			track.materialScrollValue = value;

			Vec4 programValue;
			if( EntitySystemWorld.Instance.Simulation )
				programValue = new Vec4( track.materialScrollValue.X, track.materialScrollValue.Y, 0, 0 );
			else
				programValue = Vec4.Zero;

			foreach( MeshObject.SubObject subObject in track.meshObject.SubObjects )
			{
				//update SubObject dynamic gpu parameter
				subObject.SetCustomGpuParameter(
					(int)ShaderBaseMaterial.GpuParameters.diffuse1MapTransformAdd, programValue );
			}
		}

		//Minefield specific
		void TickMinefields()
		{
			minefieldUpdateTimer -= TickDelta;
			if( minefieldUpdateTimer > 0 )
				return;
			minefieldUpdateTimer += 1;

			if( chassisBody != null && chassisBody.LinearVelocity != Vec3.Zero )
			{
				Minefield minefield = Minefield.GetMinefieldByPosition( Position );
				if( minefield != null )
				{
					Die();
				}
			}
		}

	}
}
