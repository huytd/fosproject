// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="GameWorld"/> entity type.
	/// </summary>
	public class GameWorldType : WorldType
	{
	}

	public class GameWorld : World
	{
		static GameWorld instance;

		bool needDoActionsAfterMapCreated;

		//for moving player character between maps
		string shouldChangeMapName;
		string shouldChangeMapSpawnPointName;
		PlayerCharacter.ChangeMapInformation shouldChangeMapPlayerCharacterInformation;

		bool needWorldDestroy;

		//

		GameWorldType _type = null; public new GameWorldType Type { get { return _type; } }

		public GameWorld()
		{
			instance = this;
		}

		public static new GameWorld Instance
		{
			get { return instance; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			AddTimer();

			if( EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Server ||
				EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Single )
			{
				if( PlayerManager.Instance == null )
				{
					PlayerManager manager = (PlayerManager)Entities.Instance.Create(
						"PlayerManager", this );
					manager.PostCreate();
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();

			instance = null;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( needDoActionsAfterMapCreated )
			{
				DoActionsAfterMapCreated();
				needDoActionsAfterMapCreated = false;
			}

			//recreate player units if need
			if( GameMap.Instance.GameType == GameMap.GameTypes.Action ||
				GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade )
			{
				if( PlayerManager.Instance != null )
				{
					foreach( Entity entity in PlayerManager.Instance.Children )
					{
						Player player = entity as Player;
						if( player == null )
							continue;

						if( player.Intellect == null || player.Intellect.ControlledObject == null )
							CreatePlayerUnit( player );
					}
				}
			}
		}

		void DoActionsAfterMapCreated()
		{
			if( EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Single )
			{
				if( GameMap.Instance.GameType == GameMap.GameTypes.Action ||
					GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade )
				{
					string playerName = "__PlayerName__";

					//create Player
					Player player = PlayerManager.Instance.GetPlayerByName( playerName );
					if( player == null )
						player = PlayerManager.Instance.AddSinglePlayer( playerName );

					//create PlayerIntellect
					PlayerIntellect intellect = PlayerIntellect.Instance;
					if( intellect == null )
					{
						intellect = (PlayerIntellect)Entities.Instance.Create(
							EntityTypes.Instance.GetByName( "PlayerIntellect" ), player );
						intellect.PostCreate();
					}

					player.Intellect = intellect;
					intellect.Player = player;

					//create unit
					if( intellect.ControlledObject == null )
					{
						SpawnPoint spawnPoint = null;
						if( shouldChangeMapSpawnPointName != null )
						{
							spawnPoint = Entities.Instance.GetByName( shouldChangeMapSpawnPointName )
								as SpawnPoint;
							if( spawnPoint == null )
							{
								Log.Error( "GameWorld: SpawnPoint with name \"{0}\" is not defined.",
									shouldChangeMapSpawnPointName );
							}
						}

						Unit unit;
						if( spawnPoint != null )
							unit = CreatePlayerUnit( player, spawnPoint );
						else
							unit = CreatePlayerUnit( player );

						if( unit != null )
						{
							unit.Intellect = intellect;
							intellect.ControlledObject = unit;
						}

						if( shouldChangeMapPlayerCharacterInformation != null )
						{
							( (PlayerCharacter)intellect.ControlledObject ).ApplyChangeMapInformation(
								shouldChangeMapPlayerCharacterInformation, spawnPoint );
						}
						else
						{
							if( unit != null )
							{
								intellect.LookDirection = SphereDir.FromVector(
									unit.Rotation.GetForward() );
							}
						}
					}
				}

				EntitySystemWorld.Instance.ResetExecutedTime();
			}

			shouldChangeMapName = null;
			shouldChangeMapSpawnPointName = null;
			shouldChangeMapPlayerCharacterInformation = null;
		}

		Unit CreatePlayerUnit( Player player, SpawnPoint spawnPoint )
		{
			string unitTypeName;
			if( !player.Bot )
                unitTypeName = "trautinh";
                //unitTypeName = "Rabbit";
			else
				unitTypeName = player.PlayerName;

			EntityType unitType = EntityTypes.Instance.GetByName( unitTypeName );

			Unit unit = (Unit)Entities.Instance.Create( unitType, Map.Instance );

			Vec3 posOffset = new Vec3( 0, 0, 1.5f );//!!!!temp
			unit.Position = spawnPoint.Position + posOffset;
			unit.Rotation = spawnPoint.Rotation;
			unit.PostCreate();

			if( player.Intellect != null )
			{
				player.Intellect.ControlledObject = unit;
				unit.Intellect = player.Intellect;
			}

			return unit;
		}

		Unit CreatePlayerUnit( Player player )
		{
			SpawnPoint spawnPoint = SpawnPoint.GetDefaultSpawnPoint();

			if( spawnPoint == null )
				spawnPoint = SpawnPoint.GetFreeRandomSpawnPoint();

			if( spawnPoint == null )
				return null;
			return CreatePlayerUnit( player, spawnPoint );
		}

		public string ShouldChangeMapName
		{
			get { return shouldChangeMapName; }
		}

		public string ShouldChangeMapSpawnPointName
		{
			get { return shouldChangeMapSpawnPointName; }
		}

		public void SetShouldChangeMap( string mapName, string spawnPointName,
			PlayerCharacter.ChangeMapInformation playerCharacterInformation )
		{
			if( shouldChangeMapName != null )
				return;
			shouldChangeMapName = mapName;
			shouldChangeMapSpawnPointName = spawnPointName;
			shouldChangeMapPlayerCharacterInformation = playerCharacterInformation;
		}

		protected override void OnAddChild( Entity entity )
		{
			base.OnAddChild( entity );

			if( entity is Map )
				needDoActionsAfterMapCreated = true;
		}

		[Browsable( false )]
		public bool NeedWorldDestroy
		{
			get { return needWorldDestroy; }
			set { needWorldDestroy = value; }
		}

	}
}
