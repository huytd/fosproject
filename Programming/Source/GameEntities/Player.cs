// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
		[FieldSerialize]
		string playerName;
		int ping;
		[FieldSerialize]
		int frags;

		[FieldSerialize]
		bool bot;

		////server
		//bool needClientUpdate;

		[FieldSerialize]
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

		////server
		//public bool NeedClientUpdate
		//{
		//   get { return needClientUpdate; }
		//   set { needClientUpdate = value; }
		//}

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
	}
}
