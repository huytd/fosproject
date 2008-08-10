// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Diagnostics;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.Renderer;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Fan"/> entity type.
	/// </summary>
	public class FanType : DynamicType
	{
	}

	/// <summary>
	/// Defines a fans.
	/// </summary>
	/// <remarks>
	/// It is necessary that in physical model there 
	/// was a <see cref="Engine.PhysicsSystem.GearedMotor"/> with a name "bladesMotor". 
	/// </remarks>
	public class Fan : Dynamic
	{
		[FieldSerialize]
		float forceMaximum = 50;

		[FieldSerialize]
		Vec3 influenceRegionScale = new Vec3( 20, 3, 3 );

		[FieldSerialize]
		float throttle = 1;

		InfluenceRegion region;

		GearedMotor bladesMotor;

		//

		FanType _type = null; public new FanType Type { get { return _type; } }

		/// <summary>
		/// Gets or sets the the maximal pushing force.
		/// </summary>
		[Description( "The the maximal pushing force." )]
		public float ForceMaximum
		{
			get { return forceMaximum; }
			set { forceMaximum = value; }
		}

		/// <summary>
		/// Gets or sets the current power.
		/// </summary>
		[Description( "The current power." )]
		public float Throttle
		{
			get { return throttle; }
			set
			{
				if( value < -1 || value > 1 )
					throw new InvalidOperationException( "Throttle need in [-1,1] interval" );
				throttle = value;
			}
		}

		[DefaultValue( typeof( Vec3 ), "20 3 3" )]
		public Vec3 InfluenceRegionScale
		{
			get { return influenceRegionScale; }
			set
			{
				influenceRegionScale = value;

				if( region != null )
					region.SetTransform( Position, Rotation, InfluenceRegionScale );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			const string regionTypeName = "ManualInfluenceRegion";

			InfluenceRegionType regionType = (InfluenceRegionType)EntityTypes.Instance.GetByName(
				regionTypeName );
			if( regionType == null )
			{
				regionType = (InfluenceRegionType)EntityTypes.Instance.ManualCreateType( regionTypeName,
					EntityTypes.Instance.GetClassInfoByEntityClassName( "InfluenceRegion" ) );
			}

			region = (InfluenceRegion)Entities.Instance.Create( regionType, Map.Instance );
			region.ShapeType = Region.ShapeTypes.Capsule;
			region.DistanceFunction = InfluenceRegion.DistanceFunctionType.NormalFadeAxisX;

			region.SetTransform( Position, Rotation, InfluenceRegionScale );
			region.PostCreate();
			region.AllowSave = false;
			region.EditorSelectable = false;

			bladesMotor = PhysicsModel.GetMotor( "bladesMotor" ) as GearedMotor;

			AddTimer();

			UpdateParticlesForceCoefficient( 0 );
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
				region.SetTransform( Position, Rotation, InfluenceRegionScale );
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			bladesMotor.Throttle = throttle;

			HingeJoint joint = bladesMotor.Joint as HingeJoint;
			Trace.Assert( joint != null );

			Radian jointVelocity = joint.Axis.Velocity;

			float velCoef = jointVelocity / bladesMotor.MaxVelocity;
			MathFunctions.Clamp( ref velCoef, -1.0f, 1.0f );

			region.ImpulsePerSecond = forceMaximum;
			region.Force = velCoef;

			UpdateParticlesForceCoefficient( velCoef );
		}

		void UpdateParticlesForceCoefficient( float forceCoefficient )
		{
			//Set ForceCoefficient to particles
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedParticle attachedParticleObject =
					attachedObject as MapObjectAttachedParticle;
				if( attachedParticleObject != null )
					attachedParticleObject.ForceCoefficient = forceCoefficient;
			}
		}
	}
}
