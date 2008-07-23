// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine.MapSystem;
using Engine.SoundSystem;

namespace GameEntities
{
	/// <summary>
	/// Defines possible substances of <see cref="Dynamic"/> objects.
	/// Substances are necessary for work of influences (<see cref="Influence"/>). 
	/// The certain influences operate only on the set substances.
	/// </summary>
	[Flags]
	public enum Substance
	{
		None = 0,
		Flesh = 2,
		Metal = 4,
		Wood = 8,
	}

	/// <summary>
	/// User defined Map filter groups.
	/// </summary>
	public class GameFilterGroups
	{
		public const Map.FilterGroups UnitFilterGroup = Map.FilterGroups.Group1;
	}

	/*!!!!!!
	enum
	{
		OSPT__FIX_ = 10000,

		//Player
		OSPT_PLAYER_UPDATE,

		//PlayerManager (to server)
		OSPT_PLAYER_MANAGER_NEED_UPDATE_FROM_SERVER,

		//GameWorld
		OSPT_SET_PLAYER_CONTROLLED_INTELLECT,//(BObject)

		//Intellect (to server)
		OSPT_CONTROL_KEY_PRESS, //(uint8 key !!!!! ÿþúð int)
		OSPT_CONTROL_KEY_RELEASE, //(uint8 key !!!!! ÿþúð int)

		//Unit
		OSPT_RESET_INTELLECT, //
		OSPT_SET_INTELLECT, //(Intellect)

		//PlayerIntellect (to server)
		OSPT_SET_LOOK_DIRECTION, //(Vec3)

		//Dynamic
		OSPT_SET_LIFE //(float)
	};
	*/
}
