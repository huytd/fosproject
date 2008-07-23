// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using Engine.SoundSystem;
using Engine.Utils;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Dynamic"/> entity type.
	/// </summary>
	public class DynamicType : MapObjectType
	{
		[FieldSerialize]
		float lifeMax;
		[FieldSerialize]
		float lifeMin;

		[FieldSerialize]
		float impulseDamageCoefficient;
		[FieldSerialize]
		float impulseMinimalDamage;

		[FieldSerialize]
		float targetPriority;

		[FieldSerialize]
		string soundCollision;
		[FieldSerialize]
		float soundCollisionMinVelocity = 1.0f;

		[FieldSerialize]
		Substance substance;

		[FieldSerialize]
		float dieLatency;

		[FieldSerialize]
		float lifeTime;

		[FieldSerialize]
		List<AutomaticInfluenceItem> automaticInfluences = new List<AutomaticInfluenceItem>();

		///////////////////////////////////////////

		public class AutomaticInfluenceItem
		{
			[FieldSerialize]
			InfluenceType influence;
			[FieldSerialize]
			[DefaultValue( typeof( Range ), "0 0.5" )]
			Range lifeCoefficientRange = new Range( 0, .5f );

			public InfluenceType Influence
			{
				get { return influence; }
				set { influence = value; }
			}

			[DefaultValue( typeof( Range ), "0 0.5" )]
			public Range LifeCoefficientRange
			{
				get { return lifeCoefficientRange; }
				set { lifeCoefficientRange = value; }
			}

			public override string ToString()
			{
				if( influence == null )
					return "(not initialized)";
				return influence.Name;
			}
		}

		///////////////////////////////////////////

		[EditorBrowsable( EditorBrowsableState.Never )]
		public class AutomaticInfluencesCollectionEditor : PropertyGridUtils.ModalDialogCollectionEditor
		{
			public AutomaticInfluencesCollectionEditor()
				: base( typeof( List<AutomaticInfluenceItem> ) )
			{ }
		}

		///////////////////////////////////////////

		[DefaultValue( 0.0f )]
		public float LifeMax
		{
			get { return lifeMax; }
			set { lifeMax = value; }
		}

		[DefaultValue( 0.0f )]
		public float LifeMin
		{
			get { return lifeMin; }
			set { lifeMin = value; }
		}

		[DefaultValue( 0.0f )]
		public float ImpulseDamageCoefficient
		{
			get { return impulseDamageCoefficient; }
			set { impulseDamageCoefficient = value; }
		}

		[DefaultValue( 0.0f )]
		public float ImpulseMinimalDamage
		{
			get { return impulseMinimalDamage; }
			set { impulseMinimalDamage = value; }
		}

		[DefaultValue( 0.0f )]
		public float TargetPriority
		{
			get { return targetPriority; }
			set { targetPriority = value; }
		}

		[DefaultValue( "" )]
		[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
		public string SoundCollision
		{
			get { return soundCollision; }
			set { soundCollision = value; }
		}

		[DefaultValue( 1.0f )]
		public float SoundCollisionMinVelocity
		{
			get { return soundCollisionMinVelocity; }
			set { soundCollisionMinVelocity = value; }
		}

		[DefaultValue( Substance.None )]
		public Substance Substance
		{
			get { return substance; }
			set { substance = value; }
		}

		[DefaultValue( 0.0f )]
		public float DieLatency
		{
			get { return dieLatency; }
			set { dieLatency = value; }
		}

		[DefaultValue( 0.0f )]
		public float LifeTime
		{
			get { return lifeTime; }
			set { lifeTime = value; }
		}

		[TypeConverter( typeof( CollectionTypeConverter ) )]
		[Editor( typeof( AutomaticInfluencesCollectionEditor ), typeof( UITypeEditor ) )]
		public List<AutomaticInfluenceItem> AutomaticInfluences
		{
			get { return automaticInfluences; }
		}
	}

	/// <summary>
	/// Defines a object with a lifespan, sounds and damage for the collision of physical bodies, 
	/// and management of influences.
	/// </summary>
	public class Dynamic : MapObject
	{
		[FieldSerialize]
		float createLifeCoefficient = 1.0f;
		[FieldSerialize]
		float life;

		float dieLatencyRamainingTime;

		bool died;

		float soundCollisionTimeRemaining;
		bool soundCollisionTimeRemainingTimerAdded;

		//currently only for Type.LifeTime
		[FieldSerialize]
		float lifeTime;

		float receiveDamageCoefficient = 1;

		Influence[] automaticInfluences;

		//

		DynamicType _type = null; public new DynamicType Type { get { return _type; } }

		[Browsable( false )]
		[LogicSystemBrowsable( true )]
		public float Life
		{
			get { return life; }
			set
			{
				if( life == value )
					return;

				life = value;

				if( life < Type.LifeMin )
					life = Type.LifeMin;
				else if( life > Type.LifeMax )
					life = Type.LifeMax;

				if( Type.AutomaticInfluences.Count != 0 )
					UpdateAutomaticInfluences();
			}
		}

		//!!!!!!!need Shape shape too. But currently logic system not support physics classes.
		public delegate void DamageDelegate( Dynamic entity, MapObject prejudicial,
			Vec3 pos, float damage );
		[LogicSystemBrowsable( true )]
		public event DamageDelegate Damage;

		protected virtual void OnDamage( MapObject prejudicial, Vec3 pos, Shape shape, float damage,
			bool allowMoveDamageToParent )
		{
			float realDamage = damage * ReceiveDamageCoefficient;

			if( Type.LifeMax != 0 )
			{
				float newLife = Life - realDamage;
				MathFunctions.Clamp( ref newLife, Type.LifeMin, Type.LifeMax );
				Life = newLife;

				if( Life == 0 )
					Die( prejudicial );
			}
			else
			{
				if( allowMoveDamageToParent )
				{
					//damage to parent
					Dynamic dynamic = AttachedMapObjectParent as Dynamic;
					if( dynamic != null )
						dynamic.OnDamage( prejudicial, pos, shape, realDamage, true );
				}
			}

			if( Damage != null )
				Damage( this, prejudicial, pos, realDamage );
		}

		public void DoDamage( MapObject prejudicial, Vec3 pos, Shape shape, float damage,
			bool allowMoveDamageToParent )
		{
			OnDamage( prejudicial, pos, shape, damage, allowMoveDamageToParent );
		}

		public void Die( MapObject prejudicial, bool allowLatencyTime )
		{
			if( dieLatencyRamainingTime != 0 )
				return;
			if( allowLatencyTime )
			{
				dieLatencyRamainingTime = Type.DieLatency;
				if( dieLatencyRamainingTime > 0 )
				{
					AddTimer();
					return;
				}
			}

			if( died )
				return;

			died = true;

			OnDie( prejudicial );
			SetShouldDelete();
		}

		public void Die( MapObject prejudicial )
		{
			Die( prejudicial, true );
		}

		[LogicSystemBrowsable( true )]
		public void Die()
		{
			Die( null );
		}

		[Browsable( false )]
		public bool Died
		{
			get { return died; }
		}


		protected virtual void OnCreateInfluence( Influence influence )
		{
			//AutomaticInfluences
			for( int n = 0; n < Type.AutomaticInfluences.Count; n++ )
			{
				DynamicType.AutomaticInfluenceItem typeItem = Type.AutomaticInfluences[ n ];
				if( typeItem.Influence == influence.Type )
				{
					if( automaticInfluences == null )
						automaticInfluences = new Influence[ Type.AutomaticInfluences.Count ];
					automaticInfluences[ n ] = influence;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			createLifeCoefficient = 1;
			life = Type.LifeMax * createLifeCoefficient;

			if( PhysicsModel != null )
			{
				if( Type.ImpulseDamageCoefficient != 0 || Type.SoundCollision != null )
				{
					foreach( Body body in PhysicsModel.Bodies )
						body.Collision += Body_Collision;
				}
			}

			if( Type.AutomaticInfluences.Count != 0 )
				UpdateAutomaticInfluences();

			if( Type.LifeTime != 0 )
				AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( PhysicsModel != null )
			{
				if( Type.ImpulseDamageCoefficient != 0 || Type.SoundCollision != null )
				{
					foreach( Body body in PhysicsModel.Bodies )
						body.Collision -= Body_Collision;
				}
			}
			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRemoveChild(Entity)"/></summary>
		protected override void OnRemoveChild( Entity entity )
		{
			base.OnRemoveChild( entity );

			//automaticInfluences
			Influence influence = entity as Influence;
			if( influence != null && automaticInfluences != null )
			{
				for( int n = 0; n < automaticInfluences.Length; n++ )
				{
					if( automaticInfluences[ n ] == influence )
						automaticInfluences[ n ] = null;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( dieLatencyRamainingTime != 0 )
			{
				dieLatencyRamainingTime -= TickDelta;
				if( dieLatencyRamainingTime <= 0 )
				{
					dieLatencyRamainingTime = 0;
					Die( null, false );
				}
			}

			if( Position.Z < -200.0f )
				Die();

			if( soundCollisionTimeRemaining != 0 )
			{
				soundCollisionTimeRemaining -= TickDelta;
				if( soundCollisionTimeRemaining < 0 )
				{
					soundCollisionTimeRemaining = 0;

					if( soundCollisionTimeRemainingTimerAdded )
					{
						RemoveTimer();
						soundCollisionTimeRemainingTimerAdded = false;
					}
				}
			}

			if( Type.LifeTime != 0 )
			{
				lifeTime += TickDelta;
				if( lifeTime >= Type.LifeTime )
					OnLifeTimeIsOver();
			}
		}

		protected virtual void OnDie( MapObject prejudicial ) { }

		public void AddInfluence( InfluenceType influenceType, float time, bool checkSubstance )
		{
			Trace.Assert( time > 0 );

			if( checkSubstance )
				if( influenceType.AllowSubstance != Substance.None )
					if( ( influenceType.AllowSubstance & Type.Substance ) == 0 )
						return;

			foreach( Entity child in Children )
			{
				if( child.Type != influenceType )
					continue;
				if( child.IsSetDeleted )
					continue;

				Influence i = (Influence)child;
				i.RemainingTime += time;
				return;
			}

			Influence influence = (Influence)Entities.Instance.Create( influenceType, this );
			influence.RemainingTime = time;
			influence.PostCreate();

			OnCreateInfluence( influence );
		}

		void CopyInfluencesToObject( Dynamic destination )
		{
			foreach( Entity child in Children )
			{
				if( child.IsSetDeleted )
					continue;

				Influence influence = child as Influence;
				if( influence == null )
					continue;

				Influence i = (Influence)Entities.Instance.Create( influence.Type, destination );
				i.RemainingTime = influence.RemainingTime;
				i.PostCreate();
			}
		}

		void Body_Collision( ref CollisionEvent collisionEvent )
		{
			//Type.SoundCollision
			if( soundCollisionTimeRemaining == 0 && Type.SoundCollision != null )
			{
				Body thisBody = collisionEvent.ThisShape.Body;
				Body otherBody = collisionEvent.OtherShape.Body;

				Vec3 velocityDifference = thisBody.LastStepLinearVelocity;
				if( !otherBody.Static )
					velocityDifference -= otherBody.LastStepLinearVelocity;
				else
					velocityDifference -= thisBody.LinearVelocity;

				float minVelocity = Type.SoundCollisionMinVelocity;

				bool allowPlay = velocityDifference.LengthSqr() > minVelocity * minVelocity;

				if( allowPlay )
				{
					SoundPlay3D( Type.SoundCollision, .5f, false );
					soundCollisionTimeRemaining = .25f;

					if( !soundCollisionTimeRemainingTimerAdded )
					{
						AddTimer();
						soundCollisionTimeRemainingTimerAdded = true;
					}
				}
			}

			//Type.ImpulseDamageCoefficient
			if( Type.ImpulseDamageCoefficient != 0 && Life > Type.LifeMin )
			{
				Body thisBody = collisionEvent.ThisShape.Body;
				Body otherBody = collisionEvent.OtherShape.Body;
				float otherMass = otherBody.Mass;

				float impulse = 0;
				impulse += thisBody.LastStepLinearVelocity.LengthFast() * thisBody.Mass;
				if( otherMass != 0 )
					impulse += otherBody.LastStepLinearVelocity.LengthFast() * otherMass;

				float damage = impulse * Type.ImpulseDamageCoefficient;
				if( damage >= Type.ImpulseMinimalDamage )
					OnDamage( null, collisionEvent.Position, collisionEvent.ThisShape, damage, true );
			}
		}

		public Unit GetIntellectedRootUnit()
		{
			MapObject mapObject = this;
			while( mapObject != null )
			{
				Unit unit = mapObject as Unit;
				if( unit != null && unit.Intellect != null )
					return unit;
				mapObject = mapObject.AttachedMapObjectParent;
			}
			return null;
		}

		public Unit GetParentUnit()
		{
			MapObject mapObject = this;
			while( mapObject != null )
			{
				Unit unit = mapObject as Unit;
				if( unit != null )
					return unit;
				mapObject = mapObject.AttachedMapObjectParent;
			}
			return null;
		}

		public void SoundPlay3D( string name, float priority, bool needAttach )
		{
			if( string.IsNullOrEmpty( name ) )
				return;

			if( EngineApp.Instance.DefaultSoundChannelGroup != null &&
				EngineApp.Instance.DefaultSoundChannelGroup.Volume == 0 )
				return;

			//2d sound mode for FPS Camera Player
			PlayerIntellect playerIntellect = PlayerIntellect.Instance;

			if( playerIntellect != null && playerIntellect.FPSCamera &&
				 playerIntellect.ControlledObject != null &&
				 playerIntellect.ControlledObject == GetParentUnit() )
			{
				Sound sound = SoundWorld.Instance.SoundCreate( name, 0 );
				if( sound != null )
				{
					SoundWorld.Instance.SoundPlay( sound, EngineApp.Instance.DefaultSoundChannelGroup,
						priority );
				}
				return;
			}

			//Default 3d mode
			{
				if( !needAttach )
				{
					Sound sound = SoundWorld.Instance.SoundCreate( name, SoundMode.Mode3D );
					if( sound == null )
						return;

					VirtualChannel channel = SoundWorld.Instance.SoundPlay( sound,
						EngineApp.Instance.DefaultSoundChannelGroup, priority, true );
					if( channel != null )
					{
						channel.Position = Position;
						channel.Pause = false;
					}
				}
				else
				{
					MapObjectAttachedSound attachedSound = new MapObjectAttachedSound();
					attachedSound.SetSoundName( name, false );
					Attach( attachedSound );
				}
			}

		}

		protected override void OnDieObjectCreate( MapObjectCreateObject createObject, 
			object objectCreated )
		{
			base.OnDieObjectCreate( createObject, objectCreated );

			MapObjectCreateMapObject createMapObject = createObject as MapObjectCreateMapObject;
			if( createMapObject != null )
			{
				MapObject mapObject = (MapObject)objectCreated;

				//Copy information to dead object
				//if( Type.Name + "Dead" == mapObject.Type.Name )
				if( createMapObject.CopyVelocitiesFromParent )
				{
					Dynamic dynamic = mapObject as Dynamic;
					if( dynamic != null )
						CopyInfluencesToObject( dynamic );
				}

				//random rotation
				if( createMapObject.Alias == "randomRotation" )
				{
					Bullet bullet = mapObject as Bullet;
					if( bullet != null )
					{
						bullet.Rotation = new Angles(
							World.Instance.Random.NextFloat() * 180.0f,
							World.Instance.Random.NextFloat() * 180.0f,
							World.Instance.Random.NextFloat() * 180.0f ).ToQuat();

						bullet.Velocity = bullet.Rotation.GetForward() * bullet.Type.Velocity;
					}
				}
			}
		}

		protected virtual void OnLifeTimeIsOver()
		{
			Die();
		}

		[Browsable( false )]
		public float ReceiveDamageCoefficient
		{
			get { return receiveDamageCoefficient; }
			set { receiveDamageCoefficient = value; }
		}

		void UpdateAutomaticInfluences()
		{
			if( EntitySystemWorld.Instance.WorldSimulationType == WorldSimulationType.Editor )
				return;

			for( int n = 0; n < Type.AutomaticInfluences.Count; n++ )
			{
				DynamicType.AutomaticInfluenceItem typeItem = Type.AutomaticInfluences[ n ];

				bool need =
					life >= typeItem.LifeCoefficientRange.Minimum * Type.LifeMax &&
					life <= typeItem.LifeCoefficientRange.Maximum * Type.LifeMax;

				if( need )
				{
					if( automaticInfluences == null || automaticInfluences[ n ] == null )
						AddInfluence( typeItem.Influence, 1000000.0f, false );
				}
				else
				{
					if( automaticInfluences != null && automaticInfluences[ n ] != null )
						automaticInfluences[ n ].SetShouldDelete();
				}
			}
		}

		protected virtual void OnSetForceAnimationState( string animationName ) { }

		public void SetForceAnimationState( string animationName )
		{
			OnSetForceAnimationState( animationName );
		}

	}
}
