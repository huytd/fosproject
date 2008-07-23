// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="FloatSwitch"/> entity type.
	/// </summary>
	public class FloatSwitchType : SwitchType
	{
		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float changeVelocity;

		[DefaultValue( 1.0f )]
		public float ChangeVelocity
		{
			get { return changeVelocity; }
			set { changeVelocity = value; }
		}
	}

	/// <summary>
	/// Defines the user quantitative switches.
	/// </summary>
	public class FloatSwitch : Switch
	{
		[FieldSerialize]
		float value;

		bool use;
		bool useChangeIncrease;

		FloatSwitchType _type = null; public new FloatSwitchType Type { get { return _type; } }

		[DefaultValue( 0.0f )]
		[LogicSystemBrowsable( true )]
		public float Value
		{
			get { return this.value; }
			set
			{
				if( this.value == value )
					return;

				if( value < 0 || value > 1 )
					throw new Exception( "Invalid Value Range" );

				this.value = value;

				OnValueChange();
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( use )
			{
				float step = Type.ChangeVelocity * TickDelta;

				if( useChangeIncrease )
				{
					float newValue = value + step;
					if( newValue > 1 )
						newValue = 1;
					Value = newValue;

					if( value == 1 )
						use = false;
				}
				else
				{
					float newValue = value - step;
					if( newValue < 0 )
						newValue = 0;
					Value = newValue;

					if( value == 0 )
						use = false;
				}
			}
		}

		public void UseStart()
		{
			if( use )
				return;

			use = true;

			useChangeIncrease = !useChangeIncrease;

			if( useChangeIncrease && value == 1 )
				useChangeIncrease = false;
			if( !useChangeIncrease && value == 0 )
				useChangeIncrease = true;
		}

		public void UseEnd()
		{
			use = false;
		}

	}
}
