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
using Engine.Renderer;
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
		[FieldSerialize( FieldSerializeSerializationTypes.World )]
		float life;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float dieLatencyRamainingTime;

		bool died;

		float soundCollisionTimeRemaining;
		bool soundCollisionTimeRemainingTimerAdded;

		//currently only for Type.LifeTime
		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float lifeTime;

		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float receiveDamageCoefficient = 1;

		[FieldSerialize]
		Influence[] automaticInfluences;

		//animation
		const float animationsBlendTime = .1f;

		MapObjectAttachedMesh attachedMeshForAnimation;
		//key: animation name; value: maximum index (walk, walk2, walk3)
		Dictionary<string, int> maxAnimationIndices;
		MeshObject.AnimationState forceAnimationState;

		[FieldSerialize]
		[DefaultValue( 0.0f )]
		float lastAnimationTimePosition;

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

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnLoad(TextBlock)"/>.</summary>
		protected override bool OnLoad( TextBlock block )
		{
			if( !base.OnLoad( block ) )
				return false;

			if( EntitySystemWorld.Instance.SerializationMode == SerializationModes.Map )
				life = Type.LifeMax;

			return true;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnSave(TextBlock)"/>.</summary>
		protected override void OnSave( TextBlock block )
		{
			base.OnSave( block );

			if( EntitySystemWorld.Instance.SerializationMode == SerializationModes.World )
			{
				if( attachedMeshForAnimation != null )
					SaveAnimationStates( block );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnCreate()"/>.</summary>
		protected override void OnCreate()
		{
			base.OnCreate();

			life = Type.LifeMax;
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

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

			FindAttachedMeshForAnimation();
			if( loaded && EntitySystemWorld.Instance.SerializationMode == SerializationModes.World )
			{
				if( attachedMeshForAnimation != null )
					LoadAnimationStates( LoadingTextBlock );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnShouldDelete()"/>.</summary>
		protected override bool OnShouldDelete()
		{
			attachedMeshForAnimation = null;
			return base.OnShouldDelete();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			attachedMeshForAnimation = null;

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

			if( Position.Z < Map.Instance.InitialSceneObjectsBounds.Minimum.Z - 300.0f )
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

		protected override void OnRenderFrame()
		{
			base.OnRenderFrame();

			if( attachedMeshForAnimation != null )
				UpdateAnimations();
		}

		void FindAttachedMeshForAnimation()
		{
			MapObjectAttachedMesh attachedMesh = null;

			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				attachedMesh = attachedObject as MapObjectAttachedMesh;
				if( attachedMesh != null )
					break;
			}

			if( attachedMesh == null )
				return;
			if( attachedMesh.MeshObject == null )
				return;
			if( attachedMesh.MeshObject.AnimationStates == null )
				return;

			attachedMeshForAnimation = attachedMesh;
		}

		protected virtual void OnUpdateAnimation( ref string animationName,
			ref float animationVelocity, ref bool animationLoop, ref bool allowRandomAnimationNumber )
		{
		}

		/// <summary>
		/// Returns animation with index addition. 
		/// </summary>
		/// <remarks>
		/// Example: animationName: walk; return: 1(walk) or 2(walk2) or 3(walk3).
		/// </remarks>
		/// <param name="animationName"></param>
		/// <param name="firstAnimationIn10TimesMoreOften"></param>
		/// <returns></returns>
		protected int GetRandomAnimationNumber( string animationName,
			bool firstAnimationIn10TimesMoreOften )
		{
			if( maxAnimationIndices == null )
				maxAnimationIndices = new Dictionary<string, int>();

			int maxIndex;

			MeshObject meshObject = attachedMeshForAnimation.MeshObject;

			if( !maxAnimationIndices.TryGetValue( animationName, out maxIndex ) )
			{
				//calculate max animation index
				maxIndex = 1;
				for( int n = 2; ; n++ )
				{
					if( meshObject.GetAnimationState( animationName + n.ToString() ) != null )
						maxIndex++;
					else
						break;
				}
				maxAnimationIndices.Add( animationName, maxIndex );
			}

			int number;

			//The first animation in 10 times more often
			if( firstAnimationIn10TimesMoreOften )
			{
				number = World.Instance.Random.Next( 10 + maxIndex ) + 1 - 10;
				if( number < 1 )
					number = 1;
			}
			else
				number = World.Instance.Random.Next( maxIndex ) + 1;

			return number;
		}

		[Browsable( false )]
		public MapObjectAttachedMesh AttachedMeshForAnimation
		{
			get { return attachedMeshForAnimation; }
		}

		[Browsable( false )]
		public MeshObject.AnimationState ForceAnimationState
		{
			get { return forceAnimationState; }
		}

		[Browsable( false )]
		public MapObjectAttachedMesh.AnimationStateItem CurrentAnimationState
		{
			get
			{
				if( attachedMeshForAnimation != null )
				{
					IList<MapObjectAttachedMesh.AnimationStateItem> items =
						attachedMeshForAnimation.CurrentAnimationStates;
					for( int n = 0; n < items.Count; n++ )
					{
						MapObjectAttachedMesh.AnimationStateItem item = items[ n ];

						MeshObject.AnimationState state = item.AnimationState;

						if( item.FadeOutBlendTime == 0 )
						{
							if( !state.Loop )
							{
								if( state.TimePosition + animationsBlendTime * 2 < state.Length )
									return item;
							}
							else
								return item;
						}
					}
				}
				return null;
			}
		}

		void UpdateAnimations()
		{
			MeshObject.AnimationState animationState = null;
			float animationVelocity = 1.0f;
			bool animationLoop = false;

			//get current animation
			{
				MapObjectAttachedMesh.AnimationStateItem item = CurrentAnimationState;
				if( item != null )
				{
					animationState = item.AnimationState;
					animationVelocity = item.Velocity;
					animationLoop = item.AnimationState.Loop;
				}
			}

			//force set animation
			if( forceAnimationState != null )
			{
				if( animationState != forceAnimationState )
				{
					forceAnimationState = null;
					animationState = null;
				}
			}

			if( forceAnimationState == null )
			{
				string oldAnimationName = null;
				if( animationState != null )
					oldAnimationName = animationState.Name;

				string animationName = oldAnimationName;
				bool allowRandomAnimationNumber = false;

				OnUpdateAnimation( ref animationName, ref animationVelocity, ref animationLoop,
					ref allowRandomAnimationNumber );

				if( !string.IsNullOrEmpty( animationName ) )
				{
					//random animation number functionality
					if( allowRandomAnimationNumber )
					{
						if( !string.IsNullOrEmpty( oldAnimationName ) &&
							oldAnimationName.Length >= animationName.Length )
						{
							if( string.Compare( oldAnimationName, 0, animationName, 0,
								animationName.Length ) == 0 )
							{
								//detect rewind
								if( animationState.TimePosition < lastAnimationTimePosition )
								{
									//get random animation
									int number = GetRandomAnimationNumber( animationName, true );
									if( number != 1 )
										animationName += number.ToString();
								}
								else
								{
									//use old animation
									animationName = oldAnimationName;
								}
							}
						}
					}

					if( string.Compare( animationName, oldAnimationName ) != 0 )
					{
						animationState = attachedMeshForAnimation.MeshObject.GetAnimationState(
							animationName );
					}
				}
				else
				{
					animationState = null;
				}
			}

			attachedMeshForAnimation.ChangeCurrentAnimationState( animationState, animationVelocity,
				animationLoop, animationsBlendTime );

			//update lastAnimationTimePosition
			if( animationState != null )
				lastAnimationTimePosition = animationState.TimePosition;
			else
				lastAnimationTimePosition = 0;
		}

		public void SetForceAnimation( string animationName, bool allowRandomAnimationNumber )
		{
			if( attachedMeshForAnimation == null )
				return;

			string name = animationName;
			if( allowRandomAnimationNumber )
			{
				int number = GetRandomAnimationNumber( name, true );
				if( number != 1 )
					name += number.ToString();
			}

			forceAnimationState = attachedMeshForAnimation.MeshObject.GetAnimationState( name );

			if( forceAnimationState != null )
			{
				//reset time for animation
				attachedMeshForAnimation.RemoveCurrentAnimationState( forceAnimationState, 0 );
				attachedMeshForAnimation.ChangeCurrentAnimationState( forceAnimationState,
					1, false, animationsBlendTime );
			}
		}

		void LoadAnimationStates( TextBlock block )
		{
			TextBlock itemsBlock = block.FindChild( "animationStates" );
			if( itemsBlock != null )
			{
				foreach( TextBlock itemBlock in itemsBlock.Children )
				{
					MeshObject.AnimationState animationState = attachedMeshForAnimation.MeshObject.
						GetAnimationState( itemBlock.GetAttribute( "name" ) );

					if( animationState != null )
					{
						float velocity = 0;
						if( itemBlock.IsAttributeExist( "velocity" ) )
							velocity = float.Parse( itemBlock.GetAttribute( "velocity" ) );
						bool loop = false;
						if( itemBlock.IsAttributeExist( "loop" ) )
							loop = bool.Parse( itemBlock.GetAttribute( "loop" ) );

						MapObjectAttachedMesh.AnimationStateItem item = attachedMeshForAnimation.
							AddCurrentAnimationState( animationState, velocity, loop, 0 );
						if( item != null )
						{
							if( itemBlock.IsAttributeExist( "timePosition" ) )
							{
								float timePosition = float.Parse( itemBlock.GetAttribute( "timePosition" ) );
								item.AnimationState.TimePosition = timePosition;
							}
						}
					}
				}
			}
		}

		void SaveAnimationStates( TextBlock block )
		{
			TextBlock itemsBlock = block.AddChild( "animationStates" );

			foreach( MapObjectAttachedMesh.AnimationStateItem item in
				attachedMeshForAnimation.CurrentAnimationStates )
			{
				if( item.FadeOutBlendTime != 0 )
					continue;

				TextBlock itemBlock = itemsBlock.AddChild( "item" );

				itemBlock.SetAttribute( "name", item.AnimationState.Name );
				itemBlock.SetAttribute( "velocity", item.Velocity.ToString() );
				itemBlock.SetAttribute( "loop", item.AnimationState.Loop.ToString() );
				if( !float.IsNaN( item.AnimationState.TimePosition ) )
				{
					itemBlock.SetAttribute( "timePosition", item.AnimationState.
						TimePosition.ToString() );
				}
			}

			if( forceAnimationState != null )
				block.SetAttribute( "forceAnimationState", forceAnimationState.Name );
		}

	}
}
