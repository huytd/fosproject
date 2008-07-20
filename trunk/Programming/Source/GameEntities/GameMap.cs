// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="GameMap"/> entity type.
	/// </summary>
	public class GameMapType : MapType
	{
	}

	public class GameMap : Map
	{
		static GameMap instance;

		[FieldSerialize]
		[DefaultValue( GameMap.GameTypes.Action )]
		GameTypes gameType = GameTypes.Action;

		///////////////////////////////////////////

		public enum GameTypes
		{
			None,
			Action,
			RTS,
			TPSArcade,

			//Put here your game type.
		}

		///////////////////////////////////////////

		GameMapType _type = null; public new GameMapType Type { get { return _type; } }

		public GameMap()
		{
			instance = this;
		}

		public static new GameMap Instance
		{
			get { return instance; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();
			instance = null;
		}

		[DefaultValue( GameMap.GameTypes.Action )]
		public GameTypes GameType
		{
			get { return gameType; }
			set { gameType = value; }
		}

	}
}
