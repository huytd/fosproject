// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Design;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;
using Engine.MathEx;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Item"/> entity type.
	/// </summary>
	public class ItemType : DynamicType
	{
		[FieldSerialize]
		float defaultRespawnTime;
		[FieldSerialize]
		string soundTake;

		[DefaultValue( 0.0f )]
		public float DefaultRespawnTime
		{
			get { return defaultRespawnTime; }
			set { defaultRespawnTime = value; }
		}

		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundTake
		{
			get { return soundTake; }
			set { soundTake = value; }
		}

		public ItemType()
		{
			AllowEmptyName = true;
		}
	}

	/// <summary>
	/// Items which can be picked up by units. Med-kits, weapons, ammunition.
	/// </summary>
	public class Item : Dynamic
	{
		[FieldSerialize]
		float respawnTime;

		Radian rotationAngle;

		//

		ItemType _type = null; public new ItemType Type { get { return _type; } }

		public Item()
		{
			rotationAngle = World.Instance.Random.NextFloat() * MathFunctions.PI * 2;
		}

		public float RespawnTime
		{
			get { return respawnTime; }
			set { respawnTime = value; }
		}

		protected override void OnCreate()
		{
			base.OnCreate();

			if( EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Editor )
				respawnTime = Type.DefaultRespawnTime;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			bool editor = EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Editor;

			if( !editor )
			{
				UpdateRotation();
				OldRotation = Rotation;
			}

			AddTimer();

			if( loaded && !editor && EntitySystemWorld.Instance.SerializationMode == 
				SerializationModes.Map )
			{
				ItemCreator obj = (ItemCreator)Entities.Instance.Create(
					EntityTypes.Instance.GetByName( "ItemCreator" ), Parent );
				obj.Position = Position;
				obj.ItemType = Type;
				obj.CreateRemainingTime = respawnTime;
				obj.Item = this;
				obj.PostCreate();
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			rotationAngle += TickDelta;
			UpdateRotation();
		}

		void UpdateRotation()
		{
			Rotation = new Angles( 0, 0, -rotationAngle.InDegrees() ).ToQuat();
		}

		protected virtual bool OnTake( Unit unit )
		{
			return false;
		}

		public bool Take( Unit unit )
		{
			bool ret = OnTake( unit );
			if( ret )
			{
				unit.SoundPlay3D( Type.SoundTake, .5f, true );
				Die();
			}
			return ret;
		}

	}
}
