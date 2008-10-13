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
    
    // CHANGE LOG
    // 1    VHFOS #000001     Create fable camera
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

		[FieldSerialize]
		SphereDir lookDirection;

		[FieldSerialize]
		bool fpsCamera;//for hiding player unit for the fps camera
		[FieldSerialize]
		float tpsCameraCenterOffset;

		//data for an opportunity of the player to control other units. (for Example: Turret control)
		[FieldSerialize]
		Unit mainNotActiveUnit;
		Dictionary<Shape, int> mainNotActiveUnitShapeContactGroups;
		[FieldSerialize]
		Vec3 mainNotActiveUnitRestorePosition;

		//Key update flag
        bool isForwardPressed = false;

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

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate2(bool)"/>.</summary>
		protected override void OnPostCreate2( bool loaded )
		{
			base.OnPostCreate2( loaded );

			if( loaded )
				UpdateMainControlledUnitAfterLoading();
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

			TickCurrentUnitAllowPlayerControl();
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

				float distance = 1000.0f;

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

            //VHFOS #000001 BEGIN
            //OLD CODE
            //bool needUpdate = false; 
            //END OLD CODE
			bool needUpdate = false;
            //VHFOS #000001 END

			//TPS arcade specific (camera observe)
			if( GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade )
				if( PlayerIntellect.Instance != null && !FPSCamera )
					needUpdate = false;

            needUpdate = true;

            //VHFOS #000001 BEGIN
            if (isForwardPressed)
            {
                needUpdate = true;
                isForwardPressed = false;
            }
            //VHFOS #000001 END
            
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

				
			}
		}

		void GameControlsManager_GameControlsEvent( GameControlsEventData e )
		{
			//GameControlsKeyDownEventData
			{
				GameControlsKeyDownEventData evt = e as GameControlsKeyDownEventData;
				if( evt != null )
				{
                    //VHFOS #000001 BEGIN
                    if (evt.ControlKey == GameControlKeys.Forward
                        || evt.ControlKey == GameControlKeys.Backward
                        || evt.ControlKey == GameControlKeys.Left
                        || evt.ControlKey == GameControlKeys.Right
                        )
                        isForwardPressed = true;
                    //VHFOS #000001 END

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
                    //VHFOS #000001 BEGIN
                    if (evt.ControlKey == GameControlKeys.Forward
                        || evt.ControlKey == GameControlKeys.Backward
                        || evt.ControlKey == GameControlKeys.Left
                        || evt.ControlKey == GameControlKeys.Right
                        )
                    isForwardPressed = true;
                    //VHFOS #000001 END

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
		}

		public override bool IsActive()
		{
			return true;
		}

		[Browsable( false )]
		public Unit MainNoActivedPlayerUnit
		{
			get { return mainNotActiveUnit; }
		}

		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );

			//mainNoActivedPlayerUnit destroyed
			if( mainNotActiveUnit == entity )
			{
				mainNotActiveUnit = null;
				if( ControlledObject != null )
				{
					if( !ControlledObject.IsSetDeleted )
						ControlledObject.Intellect = null;
					ControlledObject = null;
				}
				return;
			}
		}

		public void ChangeMainControlledUnit( Unit unit )
		{
			//Change player controlled unit
			mainNotActiveUnit = ControlledObject;
			if( mainNotActiveUnit != null )
			{
				AddRelationship( mainNotActiveUnit );

				mainNotActiveUnit.Intellect = null;
				mainNotActiveUnitRestorePosition = mainNotActiveUnit.Position;
				ControlledObject = unit;
				unit.Intellect = PlayerIntellect.Instance;
				unit.Destroying += AlternativeUnitAllowPlayerControl_Destroying;

				//disable collision for shapes and save contact groups
				mainNotActiveUnitShapeContactGroups = new Dictionary<Shape, int>();
				foreach( Body body in mainNotActiveUnit.PhysicsModel.Bodies )
				{
					foreach( Shape shape in body.Shapes )
					{
						mainNotActiveUnitShapeContactGroups.Add( shape, shape.ContactGroup );
						shape.ContactGroup = (int)ContactGroup.NoContact;
					}
				}
			}
		}

		void UpdateMainControlledUnitAfterLoading()
		{
			if( mainNotActiveUnit == null )
				return;

			if( ControlledObject != null )
				ControlledObject.Destroying += AlternativeUnitAllowPlayerControl_Destroying;

			//disable collision for shapes and save contact groups
			mainNotActiveUnitShapeContactGroups = new Dictionary<Shape, int>();
			foreach( Body body in mainNotActiveUnit.PhysicsModel.Bodies )
			{
				foreach( Shape shape in body.Shapes )
				{
					mainNotActiveUnitShapeContactGroups.Add( shape, shape.ContactGroup );
					shape.ContactGroup = (int)ContactGroup.NoContact;
				}
			}
		}

		void AlternativeUnitAllowPlayerControl_Destroying( Entity entity )
		{
			if( ControlledObject == entity && mainNotActiveUnit != null )
			{
				//restore main player controlled unit
				RestoreMainControlledUnit();
			}
		}

		Vec3 FindFreePositionForUnit( Unit unit, Vec3 center )
		{
			Vec3 volumeSize = unit.MapBounds.GetSize() + new Vec3( 2, 2, 0 );

			for( float zOffset = 0; ; zOffset += .3f )
			{
				for( float radius = 3; radius < 8; radius += .6f )
				{
					for( float angle = 0; angle < MathFunctions.PI * 2; angle += MathFunctions.PI / 32 )
					{
						Vec3 pos = center + new Vec3( MathFunctions.Cos( angle ),
							MathFunctions.Sin( angle ), 0 ) * radius + new Vec3( 0, 0, zOffset );

						Bounds volume = new Bounds( pos );
						volume.Expand( volumeSize * .5f );

						Body[] bodies = PhysicsWorld.Instance.VolumeCast(
							volume, (int)ContactGroup.CastOnlyContact );

						if( bodies.Length == 0 )
							return pos;
					}
				}
			}
		}

		public void RestoreMainControlledUnit()
		{
			if( mainNotActiveUnit == null )
				return;

			if( ControlledObject != null )
				ControlledObject.Intellect = null;

			mainNotActiveUnit.Position = mainNotActiveUnitRestorePosition;
		
			mainNotActiveUnit.OldPosition = mainNotActiveUnit.Position;

			mainNotActiveUnit.Visible = true;

			RemoveRelationship( mainNotActiveUnit );

			//restore contact groups for shapes
			if( mainNotActiveUnitShapeContactGroups != null )
			{
				foreach( Body body in mainNotActiveUnit.PhysicsModel.Bodies )
				{
					foreach( Shape shape in body.Shapes )
					{
						int group;
						if( mainNotActiveUnitShapeContactGroups.TryGetValue( shape, out group ) )
							shape.ContactGroup = group;
					}
				}
				mainNotActiveUnitShapeContactGroups.Clear();
				mainNotActiveUnitShapeContactGroups = null;
			}

			ControlledObject.Destroying -= AlternativeUnitAllowPlayerControl_Destroying;
			ControlledObject = mainNotActiveUnit;
			mainNotActiveUnit.Intellect = PlayerIntellect.Instance;
			mainNotActiveUnit = null;
		}

		void TickCurrentUnitAllowPlayerControl()
		{
			if( mainNotActiveUnit == null )
				return;

			mainNotActiveUnit.Position = ControlledObject.Position;
			mainNotActiveUnit.OldPosition = mainNotActiveUnit.Position;

			mainNotActiveUnit.Visible = false;

			//reset velocities
			foreach( Body body in mainNotActiveUnit.PhysicsModel.Bodies )
			{
				body.LinearVelocity = Vec3.Zero;
				body.AngularVelocity = Vec3.Zero;
			}
		}
	}
}
