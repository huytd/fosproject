// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.EntitySystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Player"/> entity type.
	/// </summary>
	public class PlayerType : EntityType
	{
	}

	public class Player : Entity
	{
		string playerName;
		int ping;
		int frags;

		bool bot;

		//server
		//!!!!!!ServerClientEntry client;//!!!!!!relations?
		bool needClientUpdate;

		Intellect intellect;

		//

		PlayerType _type = null; public new PlayerType Type { get { return _type; } }

		public string PlayerName
		{
			get { return playerName; }
			set { playerName = value; }
		}

		public int Ping
		{
			get { return ping; }
			set { ping = value; }
		}

		public int Frags
		{
			get { return frags; }
			set { frags = value; }
		}

		public bool Bot
		{
			get { return bot; }
			set { bot = value; }
		}

		//server
		public bool NeedClientUpdate
		{
			get { return needClientUpdate; }
			set { needClientUpdate = value; }
		}

		//server
		/*!!!!!!!
		public ServerClientEntry Client
		{
			get { return client; }
			set { client = value; }
		}*/

		//server
		public void UpdateClient()
		{
			//!!!!!!if( client == null)
				//return;

			/*!!!!!!
			BASSERT( IsNetworkServerObject() );
			ObjectSystemPacket packet = BeginPacket( OSPT_PLAYER_UPDATE, client );
			packet->WriteString( playerName );
			packet->WriteInt( ping );
			packet->WriteInt( frags );
			packet->End();
			*/
		}

		public Intellect Intellect
		{
			get { return intellect; }
			set
			{
				if( intellect != null )
					RemoveRelationship( intellect );
				intellect = value;
				if( intellect != null )
					AddRelationship( intellect );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );

			if( intellect == entity )
				intellect = null;
		}
			
		/*!!!!!
		protected override void OnPacket(ObjectSystemPacket& packet)
		{
			super::OnPacket(packet);

			switch(packet->GetId())
			{
			case OSPT_PLAYER_UPDATE:
				playerName = packet->ReadString();
				ping = packet->ReadInt();
				frags = packet->ReadInt();
				break;
			}
		}*/


	}
}
