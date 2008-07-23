// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.MapSystem;
using Engine.Renderer;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="PhysicalStream"/> entity type.
	/// </summary>
	public class PhysicalStreamType : DynamicType
	{
		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float length;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float thickness;

		[TypeConverter( typeof( ExpandableObjectConverter ) )]
		public class Mode
		{
			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float damagePerSecond;

			[FieldSerialize]
			InfluenceType influenceType;

			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float influenceTimePerSecond;

			[FieldSerialize]
			[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
			string particleName;

			//

			[DefaultValue( 0.0f )]
			public float DamagePerSecond
			{
				get { return damagePerSecond; }
				set { damagePerSecond = value; }
			}

			[RefreshProperties( RefreshProperties.Repaint )]
			public InfluenceType InfluenceType
			{
				get { return influenceType; }
				set { influenceType = value; }
			}

			[DefaultValue( 0.0f )]
			public float InfluenceTimePerSecond
			{
				get { return influenceTimePerSecond; }
				set { influenceTimePerSecond = value; }
			}

			[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
			[RefreshProperties( RefreshProperties.Repaint )]
			public string ParticleName
			{
				get { return particleName; }
				set { particleName = value; }
			}

			public override string ToString()
			{
				string text = "";

				if( influenceType != null )
				{
					if( text != "" )
						text += ", ";
					text += "Influence: " + influenceType.Name;
				}

				if( !string.IsNullOrEmpty( particleName ) )
				{
					if( text != "" )
						text += ", ";
					text += "Particle: " + particleName;
				}

				return text;
			}
		}

		[FieldSerialize]
		Mode normalMode = new Mode();
		[FieldSerialize]
		Mode alternativeMode = new Mode();

		[DefaultValue( 0.0f )]
		public float Length
		{
			get { return length; }
			set { length = value; }
		}

		[DefaultValue( 0.0f )]
		public float Thickness
		{
			get { return thickness; }
			set { thickness = value; }
		}

		public Mode NormalMode
		{
			get { return normalMode; }
		}

		public Mode AlternativeMode
		{
			get { return alternativeMode; }
		}
	}

	/// <summary>
	/// Defines the physics streams. You can create steam, fiery streams, etc.
	/// </summary>
	public class PhysicalStream : Dynamic
	{
		[FieldSerialize]
		bool alternativeMode;

		[FieldSerialize]
		float throttle;

		InfluenceRegion region;

		PhysicalStreamType _type = null; public new PhysicalStreamType Type { get { return _type; } }

		MapObjectAttachedParticle modeAttachedParticle;

		[DefaultValue( 0.0f )]
		[LogicSystemBrowsable( true )]
		public float Throttle
		{
			get { return throttle; }
			set
			{
				if( throttle == value )
					return;

				if( value < 0 || value > 1 )
					throw new InvalidOperationException( "Throttle need in [0,1] interval" );
				throttle = value;

				UpdateTransform();

				if( EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Editor )
					UpdateParticlesForceCoefficient();
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			//region
			const string regionTypeName = "ManualInfluenceRegion";

			InfluenceRegionType regionType = (InfluenceRegionType)EntityTypes.Instance.GetByName( regionTypeName );
			if( regionType == null )
			{
				regionType = (InfluenceRegionType)EntityTypes.Instance.ManualCreateType( regionTypeName,
					EntityTypes.Instance.GetClassInfoByEntityClassName( "InfluenceRegion" ) );
			}

			if( Type.Thickness != 0 && Type.Length != 0 )
			{
				region = (InfluenceRegion)Entities.Instance.Create( regionType, Map.Instance );
				region.ShapeType = Region.ShapeTypes.Box;
				UpdateTransform();
				region.PostCreate();
				region.AllowSave = false;
				region.EditorSelectable = false;
			}

			UpdateMode();

			AddTimer();

			UpdateParticlesForceCoefficient();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( region != null )
			{
				region.SetShouldDelete();
				region = null;
			}
			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnSetTransform(ref Vec3,ref Quat,ref Vec3)"/>.</summary>
		protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
		{
			base.OnSetTransform( ref pos, ref rot, ref scl );
			if( region != null )
				UpdateTransform();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( region != null )
				region.Force = throttle;

			UpdateParticlesForceCoefficient();
		}

		void UpdateParticlesForceCoefficient()
		{
			//Set ForceCoefficient to particles
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedParticle attachedParticleObject =
					attachedObject as MapObjectAttachedParticle;
				if( attachedParticleObject != null )
					attachedParticleObject.ForceCoefficient = Throttle;
			}
		}

		void UpdateTransform()
		{
			if( region == null )
				return;

			float len = Type.Length * throttle;

			Vec3 size = new Vec3( len, Type.Thickness, Type.Thickness );
			region.SetTransform( Position + Rotation * new Vec3( size.X * .5f, 0, 0 ), Rotation, size );
		}

		void UpdateMode()
		{
			PhysicalStreamType.Mode mode = alternativeMode ? Type.AlternativeMode : Type.NormalMode;

			if( region != null )
			{
				region.DamagePerSecond = mode.DamagePerSecond;
				region.InfluenceType = mode.InfluenceType;
				region.InfluenceTimePerSecond = mode.InfluenceTimePerSecond;
			}

			if( modeAttachedParticle != null )
			{
				Detach( modeAttachedParticle );
				modeAttachedParticle = null;
			}

			if( mode.ParticleName != null && mode.ParticleName != "" )
			{
				modeAttachedParticle = new MapObjectAttachedParticle();
				modeAttachedParticle.SetParticleSystem( mode.ParticleName );
				modeAttachedParticle.OwnerRotation = true;
				Attach( modeAttachedParticle );
			}

			UpdateParticlesForceCoefficient();
		}

		[DefaultValue( false )]
		[LogicSystemBrowsable( true )]
		public bool AlternativeMode
		{
			get { return alternativeMode; }
			set
			{
				if( alternativeMode == value )
					return;
				alternativeMode = value;
				UpdateMode();
			}
		}
		
	}
}
