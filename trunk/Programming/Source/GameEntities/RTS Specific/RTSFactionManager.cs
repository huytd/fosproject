// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="RTSFactionManager"/> entity type.
	/// </summary>
	public class RTSFactionManagerType : MapGeneralObjectType
	{
		public RTSFactionManagerType()
		{
			UniqueEntityInstance = true;
			AllowEmptyName = true;
		}
	}

	public class RTSFactionManager : MapGeneralObject
	{
		static RTSFactionManager instance;

		public class FactionItem
		{
			[FieldSerialize]
			FactionType factionType;

			[FieldSerialize]
			float money;

			//

			public FactionType FactionType
			{
				get { return factionType; }
				set { factionType = value; }
			}

			[DefaultValue( 0.0f )]
			public float Money
			{
				get { return money; }
				set { money = value; }
			}

			public override string ToString()
			{
				if( FactionType == null )
					return "(not initialized)";
				return FactionType.FullName;
			}
		}

		//!!!!temp. load/save List<custom class> not implemented
		//[FieldSerialize]
		List<FactionItem> factions = new List<FactionItem>();

		///////////////////////////////////////////

		[EditorBrowsable( EditorBrowsableState.Never )]
		public class FactionsCollectionEditor : PropertyGridUtils.ModalDialogCollectionEditor
		{
			public FactionsCollectionEditor()
				: base( typeof( List<FactionItem> ) )
			{ }
		}

		///////////////////////////////////////////

		RTSFactionManagerType _type = null; public new RTSFactionManagerType Type { get { return _type; } }

		public static RTSFactionManager Instance
		{
			get { return instance; }
		}

		public RTSFactionManager()
		{
			if( instance != null )
				Log.Fatal( "RTSFactionManager: instance != null" );
			instance = this;
		}

		/// <summary>
		/// Don't modify
		/// </summary>
		[TypeConverter( typeof( CollectionTypeConverter ) )]
		[Editor( typeof( FactionsCollectionEditor ), typeof( UITypeEditor ) )]
		public List<FactionItem> Factions
		{
			get { return factions; }
		}

		protected override bool OnLoad( TextBlock block )
		{
			//!!!!temp. load/save List<custom class> not implemented
			TextBlock group = block.FindChild( "factions" );
			if( group != null )
			{
				foreach( TextBlock itemBlock in group.Children )
				{
					FactionItem item = new FactionItem();

					item.FactionType = (FactionType)EntityTypes.Instance.GetByName( 
						itemBlock.GetAttribute( "factionType" ) );
					item.Money = float.Parse( itemBlock.GetAttribute( "money" ) );

					factions.Add( item );
				}
			}

			return base.OnLoad( block );
		}

		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			//!!!!temp. load/save List<custom class> not implemented
			TextBlock group = block.AddChild( "factions" );
			foreach( FactionItem item in factions )
			{
				TextBlock itemBlock = group.AddChild( "item" );
				if( item.FactionType != null )
					itemBlock.SetAttribute( "factionType", item.FactionType.Name );

				itemBlock.SetAttribute( "money", item.Money.ToString() );
			}

		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			if( instance == this )//for undo support
				instance = this;

			base.OnPostCreate( loaded );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			base.OnDestroy();

			if( instance == this )//for undo support
				instance = null;
		}

		public FactionItem GetFactionItemByType( FactionType type )
		{
			foreach( FactionItem item in factions )
				if( item.FactionType == type )
					return item;
			return null;
		}
	}
}
