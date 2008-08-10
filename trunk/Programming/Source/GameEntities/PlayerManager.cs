// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Engine.EntitySystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="PlayerManager"/> entity type.
	/// </summary>
	public class PlayerManagerType : EntityType
	{
	}

	public class PlayerManager : Entity
	{
		static PlayerManager instance;

		//server
		//float needUpdateClientsTime;
		//bool needUpdateAllClients;

		////client
		//float needUpdateFromServerTime;

		//

		PlayerManagerType _type = null; public new PlayerManagerType Type { get { return _type; } }

		public PlayerManager()
		{
			instance = this;
		}

		public static PlayerManager Instance
		{
			get { return instance; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			instance = this;
			base.OnPostCreate( loaded );

			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			//if( !IsEditorExcludedFromWorld() )
			instance = null;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			//if( needUpdateFromServerTime != 0 )
			//{
			//   needUpdateFromServerTime -= TickDelta;
			//   if( needUpdateFromServerTime <= 0 )
			//      needUpdateFromServerTime = 0;
			//}
		}

		//Server
		public Player AddSinglePlayer( string name )
		{
			Player player = (Player)Entities.Instance.Create(
				EntityTypes.Instance.GetByName( "Player" ), this );
			player.PostCreate();
			player.PlayerName = name;
			return player;
		}

		//Server
		public Player AddBotPlayer( string name )
		{
			Player player = (Player)Entities.Instance.Create(
				EntityTypes.Instance.GetByName( "Player" ), this );
			player.PostCreate();
			player.Bot = true;
			player.PlayerName = name;
			return player;
		}

		//Server
		public void RemovePlayer( Player player )
		{
			player.SetDeleted();
		}

		public Player GetPlayerByName( string name )
		{
			//slowly
			foreach( Entity entity in Children )
			{
				Player player = entity as Player;
				if( player == null )
					continue;

				if( player.PlayerName == name )
					return player;
			}
			return null;
		}
	}
}
