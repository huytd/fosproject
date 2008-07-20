// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.Renderer;
using GameCommon;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="PlayerIntellect"/> entity type.
	/// </summary>
	public class PlayerIntellectType : IntellectType
	{
	}

	/// <summary>
	/// Represents intellect of the player.
	/// The class accepts operating player control commands 
	/// (See <a href="EntitySystem_PlayerControl.htm">Entity System: Player Control</a>) 
	/// and passes their to base class <see cref="Intellect"/>.
	/// </summary>
	public class PlayerIntellect : Intellect
	{
		static PlayerIntellect instance;

		SphereDir lookDirection;

		bool fpsCamera;//for hiding player unit for the fps camera
		float tpsCameraCenterOffset;

		//

		PlayerIntellectType _type = null; public new PlayerIntellectType Type { get { return _type; } }

		public static PlayerIntellect Instance
		{
			get { return instance; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			if( instance != null )
				throw new Exception( "PlayerIntellect already created" );

			instance = this;

			Faction = (FactionType)EntityTypes.Instance.GetByName( "GoodFaction" );
			Trace.Assert( Faction != null );

			//This entity will accept commands of the player
			if( GameControlsManager.Instance != null )
				GameControlsManager.Instance.GameControlsEvent += GameControlsManager_GameControlsEvent;

			AllowTakeItems = true;

			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( GameControlsManager.Instance != null )
				GameControlsManager.Instance.GameControlsEvent -= GameControlsManager_GameControlsEvent;

			base.OnDestroy();
			if( !IsEditorExcludedFromWorld() )
				instance = null;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Single )
				UpdateControlledObject();
		}

		protected override void OnTickClient()
		{
			base.OnTickClient();

			//	if(GetWorld().GetPlayerIntellect() == This)
			//		UpdateLookDirection();
		}

		void UpdateControlledObject()
		{
			if( ControlledObject == null )
				return;

			//CutSceneManager specific
			if( CutSceneManager.Instance != null && CutSceneManager.Instance.CutSceneEnable )
				return;

			Vec3 lookTo;
			MapObject lookObject;
			{
				Vec3 from;
				Vec3 dir;

				if( !fpsCamera )
				{
					from = ControlledObject.Position + new Vec3( 0, 0, tpsCameraCenterOffset );
					dir = lookDirection.GetVector();
				}
				else
				{
					from = ControlledObject.Position +
						ControlledObject.Type.FPSCameraOffset * ControlledObject.Rotation;
					dir = lookDirection.GetVector();
				}

				//invalid ray
				if( dir == Vec3.Zero || float.IsNaN( from.X ) || float.IsNaN( dir.X ) )
					return;

				float distance = 1000.0f;// RendererWorld.Instance.DefaultCamera.FarClipDistance;

				lookTo = from + dir * distance;
				lookObject = null;

				RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
					new Ray( from, dir * distance ), (int)ContactGroup.CastAll );
				//new Ray( from, dir * distance ), (int)ContactGroup.CastOnlyContact );
				foreach( RayCastResult result in piercingResult )
				{
					WaterPlane waterPlane = WaterPlane.GetWaterPlaneByBody( result.Shape.Body );

					if( waterPlane == null && result.Shape.ContactGroup == (int)ContactGroup.NoContact )
						continue;

					bool ignore = false;

					MapObject obj = MapSystemWorld.GetMapObjectByBody( result.Shape.Body );

					if( obj == ControlledObject )
						ignore = true;

					if( waterPlane != null )
					{
						//ignore water from inside
						if( result.Shape.Body.GetGlobalBounds().IsContainsPoint( from ) )
							ignore = true;
					}

					//..

					if( !ignore )
					{
						lookTo = result.Position;
						lookObject = obj;
						break;
					}
				}
			}

			bool needUpdate = true;

			//TPS arcade specific (camera observe)
			if( GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade )
				if( PlayerIntellect.Instance != null && !FPSCamera )
					needUpdate = false;

			//Update units
			if( needUpdate )
			{
				//Character specific
				Character character = ControlledObject as Character;
				if( character != null )
					character.SetTurnToPosition( lookTo );

				//Turret specific
				Turret turret = ControlledObject as Turret;
				if( turret != null )
				{
					bool set = true;

					if( lookObject != null )
					{
						if( lookObject == turret )
							set = false;

						Dynamic lookObjectDynamic = lookObject as Dynamic;
						if( lookObjectDynamic != null )
						{
							if( lookObjectDynamic.GetIntellectedRootUnit() == turret )
								set = false;
						}
					}

					if( set )
						turret.SetMomentaryTurnToPosition( lookTo );
				}

				//Tank specific
				Tank tank = ControlledObject as Tank;
				if( tank != null )
				{
					bool set = true;

					if( lookObject != null )
					{
						if( lookObject == tank )
							set = false;

						Dynamic lookObjectDynamic = lookObject as Dynamic;
						if( lookObjectDynamic != null )
						{
							if( lookObjectDynamic.GetIntellectedRootUnit() == tank )
								set = false;
						}
					}

					if( set )
						tank.SetNeedTurnToPosition( lookTo );
				}
			}
		}

		void GameControlsManager_GameControlsEvent( GameControlsEventData e )
		{
			//GameControlsKeyDownEventData
			{
				GameControlsKeyDownEventData evt = e as GameControlsKeyDownEventData;
				if( evt != null )
				{
					ControlKeyPress( evt.ControlKey, evt.Strength );
					UpdateControlledObject();
					return;
				}
			}

			//GameControlsKeyUpEventData
			{
				GameControlsKeyUpEventData evt = e as GameControlsKeyUpEventData;
				if( evt != null )
				{
					ControlKeyRelease( evt.ControlKey );
					UpdateControlledObject();
					return;
				}
			}

			//GameControlsMouseMoveEventData
			{
				GameControlsMouseMoveEventData evt = e as GameControlsMouseMoveEventData;
				if( evt != null )
				{
					Vec2 sens = GameControlsManager.Instance.MouseSensitivity * 2;

					lookDirection.Horizontal -= evt.MouseOffset.X * sens.X;
					lookDirection.Vertical -= evt.MouseOffset.Y * sens.Y;

					float limit = fpsCamera ? .1f : MathFunctions.PI / 8;
					if( lookDirection.Vertical < -( MathFunctions.PI / 2 - limit ) )
						lookDirection.Vertical = -( MathFunctions.PI / 2 - limit );
					if( lookDirection.Vertical > ( MathFunctions.PI / 2 - limit ) )
						lookDirection.Vertical = ( MathFunctions.PI / 2 - limit );

					UpdateControlledObject();
					return;
				}
			}

			//GameControlsTickEventData
			{
				GameControlsTickEventData evt = e as GameControlsTickEventData;
				if( evt != null )
				{
					Vec2 sensitivity = GameControlsManager.Instance.JoystickAxesSensitivity * 2;

					Vec2 offset = Vec2.Zero;
					offset.X -= GetControlKeyStrength( GameControlKeys.LookLeft );
					offset.X += GetControlKeyStrength( GameControlKeys.LookRight );
					offset.Y += GetControlKeyStrength( GameControlKeys.LookUp );
					offset.Y -= GetControlKeyStrength( GameControlKeys.LookDown );

					//Turret specific
					if( ControlledObject != null && ControlledObject is Turret )
					{
						offset.X -= GetControlKeyStrength( GameControlKeys.Left );
						offset.X += GetControlKeyStrength( GameControlKeys.Right );
						offset.Y += GetControlKeyStrength( GameControlKeys.Forward );
						offset.Y -= GetControlKeyStrength( GameControlKeys.Backward );
					}

					offset *= evt.Delta * sensitivity;

					lookDirection.Horizontal -= offset.X;
					lookDirection.Vertical += offset.Y;

					float limit = fpsCamera ? .1f : MathFunctions.PI / 8;
					if( lookDirection.Vertical < -( MathFunctions.PI / 2 - limit ) )
						lookDirection.Vertical = -( MathFunctions.PI / 2 - limit );
					if( lookDirection.Vertical > ( MathFunctions.PI / 2 - limit ) )
						lookDirection.Vertical = ( MathFunctions.PI / 2 - limit );

					UpdateControlledObject();
					return;
				}
			}


			//..
		}

		[Browsable( false )]
		public SphereDir LookDirection
		{
			get { return lookDirection; }
			set { lookDirection = value; }
		}

		[Browsable( false )]
		public bool FPSCamera
		{
			get { return fpsCamera; }
			set { fpsCamera = value; }
		}

		[Browsable( false )]
		public float TPSCameraCenterOffset
		{
			get { return tpsCameraCenterOffset; }
			set { tpsCameraCenterOffset = value; }
		}

		protected override void OnControlledObjectChange( Unit oldObject )
		{
			base.OnControlledObjectChange( oldObject );

			//update look direction
			if( ControlledObject != null )
				lookDirection = SphereDir.FromVector( ControlledObject.Rotation * new Vec3( 1, 0, 0 ) );

			//TankGame specific
			{
				//set small damage for player tank
				Tank oldTank = oldObject as Tank;
				if( oldTank != null )
					oldTank.ReceiveDamageCoefficient = 1;
				Tank tank = ControlledObject as Tank;
				if( tank != null )
					tank.ReceiveDamageCoefficient = .1f;
			}
		}

		public override bool IsActive()
		{
			return true;
		}

	}
}
