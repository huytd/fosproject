// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.Renderer;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Intellect"/> entity type.
	/// </summary>
	public abstract class IntellectType : EntityType
	{
	}

	/// <summary>
	/// This takes the form of either AI (Artificial Intelligence) or player control over a unit .
	/// </summary>
	/// <remarks>
	/// <para>
	/// There is inherit AI base base for an computer-controlled intellect. 
	/// For example, there is the <see cref="GameCharacterAI"/> class which is designed for the 
	/// management of a game character.
	/// </para>
	/// <para>
	/// Control by a live player (<see cref="PlayerIntellect"/>) is achieved through the commands 
	/// of pressed keys or the mouse for control of the unit or turret.
	/// </para>
	/// </remarks>
	public abstract class Intellect : Entity
	{
		Unit controlledObject;
		float[] controlKeysStrength;
		Player player;
		FactionType faction;
		bool allowTakeItems;

		///////////////////////////////////////////

		public struct Command
		{
			GameControlKeys key;
			bool keyPressed;

			internal Command( GameControlKeys key, bool keyPressed )
			{
				this.key = key;
				this.keyPressed = keyPressed;
			}

			public GameControlKeys Key
			{
				get { return key; }
			}

			public bool KeyPressed
			{
				get { return keyPressed; }
			}

			public bool KeyReleased
			{
				get { return !keyPressed; }
			}
		}

		///////////////////////////////////////////

		IntellectType _type = null; public new IntellectType Type { get { return _type; } }

		public Intellect()
		{
			if( GameControlsManager.Instance != null )
				controlKeysStrength = new float[ GameControlsManager.Instance.Items.Length ];
		}

		public float GetControlKeyStrength( GameControlKeys key )
		{
			if( controlKeysStrength == null )
				return 0;
			return controlKeysStrength[ (int)key ];
		}

		public bool IsControlKeyPressed( GameControlKeys key )
		{
			return GetControlKeyStrength( key ) != 0.0f;
		}

		protected virtual void OnControlledObjectChange( Unit oldObject ) { }

		[Browsable( false )]
		public Unit ControlledObject
		{
			get { return controlledObject; }
			set
			{
				Unit oldObject = controlledObject;

				if( controlledObject != null )
					RemoveRelationship( controlledObject );
				controlledObject = value;
				if( controlledObject != null )
					AddRelationship( controlledObject );
				ResetControlKeys();

				OnControlledObjectChange( oldObject );
			}
		}

		[Browsable( false )]
		[LogicSystemBrowsable( true )]
		public FactionType Faction
		{
			get { return faction; }
			set { faction = value; }
		}

		[Browsable( false )]
		public Player Player
		{
			get { return player; }
			set
			{
				if( player != null )
					RemoveRelationship( player );
				player = value;
				if( player != null )
					AddRelationship( player );
			}
		}

		protected override void OnDestroy()
		{
			ControlledObject = null;
			base.OnDestroy();
		}

		protected void ControlKeyPress( GameControlKeys controlKey, float strength )
		{
			if( strength <= 0.0f )
				Log.Fatal( "Intellect: ControlKeyPress: Invalid \"strength\"." );

			if( GetControlKeyStrength( controlKey ) == strength )
				return;

			if( controlKeysStrength != null )
				controlKeysStrength[ (int)controlKey ] = strength;

			if( controlledObject != null )
				controlledObject.DoIntellectCommand( new Command( controlKey, true ) );
		}

		protected void ControlKeyRelease( GameControlKeys controlKey )
		{
			if( !IsControlKeyPressed( controlKey ) )
				return;
			if( controlKeysStrength != null )
				controlKeysStrength[ (int)controlKey ] = 0;

			if( controlledObject != null )
				controlledObject.DoIntellectCommand( new Command( controlKey, false ) );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );

			if( player == entity )
				player = null;
			if( controlledObject == entity )
				controlledObject = null;
		}

		void ResetControlKeys()
		{
			if( controlKeysStrength != null )
				for( int n = 0; n < controlKeysStrength.Length; n++ )
					controlKeysStrength[ n ] = 0;
		}

		protected virtual void OnRender( Camera camera ) { }

		public void DoRender( Camera camera ) { OnRender( camera ); }

		public virtual bool IsActive()
		{
			return false;
		}

		public bool AllowTakeItems
		{
			get { return allowTakeItems; }
			set { allowTakeItems = value; }
		}

	}
}
