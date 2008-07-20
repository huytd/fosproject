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

		//client;
		float needUpdateFromServerTime;

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

		//!!!!!
		////server
		//public void NeedUpdateAllClients()
		//{
		//   //!!!!!!!!!!!!закомментил а то варнинги были needUpdateAllClients = true;
		//}

		protected override void OnCreate()
		{
			base.OnCreate();
			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			instance = this;
			base.OnPostCreate( loaded );
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

			/*!!!!!!
			if(IsNetworkServerObject())
			{
				needUpdateClientsTime -= TickDelta;
				if(needUpdateClientsTime <= 0)
				{
					needUpdateClientsTime = .25f;

					//update player names
					for(ObjectLinkListIterator iter = GetChildListIterator(); iter->IsValid(); iter->Next())
					{
						Player player = Cast<Player>(iter->GetValue());
						if(!player)continue;

						if(player->GetPlayerName() != player->GetClient()->GetName() ||
							player->GetPing() != player->GetClient()->GetPing())
						{
							player->SetPlayerName(player->GetClient()->GetName());
							player->SetPing(player->GetClient()->GetPing());
							NeedUpdateAllClients();
						}
					}

					for(ObjectLinkListIterator iter = GetChildListIterator(); iter->IsValid(); iter->Next())
					{
						Player player = Cast<Player>(iter->GetValue());
						if(!player)continue;

						//update client
						if(needUpdateAllClients || player->IsNeedClientUpdate())
							player->UpdateClient();
					}
				}
			}*/

			if( needUpdateFromServerTime != 0)
			{
				needUpdateFromServerTime -= TickDelta;
				if( needUpdateFromServerTime <= 0 )
					needUpdateFromServerTime = 0;
			}
		}

		//client
		public void NeedUpdateFromServer()
		{
			/*!!!!!!
			BASSERT(IsNetworkClientObject());

			if(needUpdateFromServerTime)
				return;
			needUpdateFromServerTime = .25f;

			ObjectSystemPacket packet = BeginPacket(OSPT_PLAYER_MANAGER_NEED_UPDATE_FROM_SERVER);
			packet->End();
			*/
		}

		/*!!!!!
		protected override void OnPacket(ObjectSystemPacket packet)
		{
			super::OnPacket(packet);

			switch(packet->GetId())
			{
			//server
			case OSPT_PLAYER_MANAGER_NEED_UPDATE_FROM_SERVER:
				{
					BASSERT(IsNetworkServerObject());
					Player player = GetPlayerByClient(packet->GetClient());
					if(player)
						player->SetNeedClientUpdate();
				}
				break;
			}
		}*/

		//Server
		/*!!!!!!!!
		public Player GetPlayerByClient( ServerClientEntry client )
		{
			foreach( Entity child in Children )
			{
				Player player = child as Player;
				if( player == null )
					continue;
				if( player.Client == client )
					return player;
			}
			return null;
		}*/

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
		/*!!!!!!!!!
		public Player AddPlayer( ServerClientEntry client )
		{
			Trace.Assert( GetPlayerByClient( client ) == null );

			Player player = Entities.Instance.Create( EntityTypes.Instance.Find( "Player" ), this );
			player.PostCreate();
			player.PlayerName = client.Name;
			player.Client = client;
			return player;
		}*/

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

	}
}
