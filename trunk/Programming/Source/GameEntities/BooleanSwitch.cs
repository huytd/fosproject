// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="BooleanSwitch"/> entity type.
	/// </summary>
	public class BooleanSwitchType : SwitchType
	{
		[FieldSerialize]
		[DefaultValue("True")]
		string trueValueAttachedAlias = "True";

		[FieldSerialize]
		[DefaultValue( "False" )]
		string falseValueAttachedAlias = "False";

		[DefaultValue( "True" )]
		public string TrueValueAttachedAlias
		{
			get { return trueValueAttachedAlias; }
			set { trueValueAttachedAlias = value; }
		}

		[DefaultValue( "False" )]
		public string FalseValueAttachedAlias
		{
			get { return falseValueAttachedAlias; }
			set { falseValueAttachedAlias = value; }
		}

	}

	/// <summary>
	/// Defines the user boolean switches.
	/// </summary>
	public class BooleanSwitch : Switch
	{
		[FieldSerialize]
		bool value;

		BooleanSwitchType _type = null; public new BooleanSwitchType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			UpdateAttachedObjects();
		}

		[DefaultValue( false )]
		[LogicSystemBrowsable( true )]
		public bool Value
		{
			get { return this.value; }
			set
			{
				if( this.value == value )
					return;
				this.value = value;
				OnValueChange();
				UpdateAttachedObjects();
			}
		}

		void UpdateAttachedObjects()
		{
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				if( attachedObject.Alias == Type.TrueValueAttachedAlias )
					attachedObject.Visible = value;
				else if( attachedObject.Alias == Type.FalseValueAttachedAlias )
					attachedObject.Visible = !value;
			}
		}
		
	}
}
