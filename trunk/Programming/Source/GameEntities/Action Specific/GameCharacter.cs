// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.Renderer;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="GameCharacter"/> entity type.
	/// </summary>
	public class GameCharacterType : CharacterType
	{
		[FieldSerialize]
		[DefaultValue( typeof( Range ), "0 0" )]
		Range optimalAttackDistanceRange;

		[FieldSerialize]
		[DefaultValue( "idle" )]
		string idleAnimationName = "idle";

		[FieldSerialize]
		[DefaultValue( "walk" )]
		string walkAnimationName = "walk";

		[FieldSerialize]
		[DefaultValue( 1.0f )]
		float walkAnimationVelocityMultiplier = 1;

		[FieldSerialize]
		[DefaultValue( "jump" )]
		string jumpAnimationName = "jump";

		//

		[DefaultValue( typeof( Range ), "0 0" )]
		public Range OptimalAttackDistanceRange
		{
			get { return optimalAttackDistanceRange; }
			set { optimalAttackDistanceRange = value; }
		}

		[DefaultValue( "idle" )]
		public string IdleAnimationName
		{
			get { return idleAnimationName; }
			set { idleAnimationName = value; }
		}

		[DefaultValue( "walk" )]
		public string WalkAnimationName
		{
			get { return walkAnimationName; }
			set { walkAnimationName = value; }
		}

		[DefaultValue( 1.0f )]
		public float WalkAnimationVelocityMultiplier
		{
			get { return walkAnimationVelocityMultiplier; }
			set { walkAnimationVelocityMultiplier = value; }
		}

		[DefaultValue( "jump" )]
		public string JumpAnimationName
		{
			get { return jumpAnimationName; }
			set { jumpAnimationName = value; }
		}
	}

	public class GameCharacter : Character
	{
		MapObjectAttachedMesh mainMeshObject;

		//for animations
		const float animationsBlendTime = .1f;
		//key: animation name; value: maximum index (walk, walk2, walk3)
		Dictionary<string, int> maxAnimationIndices = new Dictionary<string, int>();
		MeshObject.AnimationState forceAnimationState;
		float lastAnimationTimePosition;

		//

		GameCharacterType _type = null; public new GameCharacterType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

			//get mainMeshObject
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
			{
				MapObjectAttachedMesh attachedMeshObject = attachedObject as MapObjectAttachedMesh;
				if( attachedMeshObject != null )
				{
					if( mainMeshObject == null )
						mainMeshObject = attachedMeshObject;
				}
			}

			AddTimer();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnShouldDelete()"/>.</summary>
		protected override bool OnShouldDelete()
		{
			mainMeshObject = null;

			return base.OnShouldDelete();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			mainMeshObject = null;
			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			//before OnTick for save old MainBody.LinearVelocity
			TickAnimations();

			base.OnTick();
		}

		/// <summary>
		/// Returns animation with index addition. 
		/// </summary>
		/// <remarks>
		/// Example: animationName: walk; return: walk or walk2 or walk3.
		/// </remarks>
		/// <param name="animationName"></param>
		/// <returns></returns>
		string GetRandomAnimationNumber( string animationName )
		{
			int maxIndex;

			if( !maxAnimationIndices.TryGetValue( animationName, out maxIndex ) )
			{
				//calculate max animation index
				maxIndex = 1;
				for( int n = 2; ; n++ )
				{
					if( mainMeshObject.MeshObject.GetAnimationState( animationName + n.ToString() ) != null )
						maxIndex++;
					else
						break;
				}
				maxAnimationIndices.Add( animationName, maxIndex );
			}

			int number;

			if( animationName == Type.IdleAnimationName || animationName == Type.WalkAnimationName )
			{
				//The first animation in 10 times more often
				number = World.Instance.Random.Next( 10 + maxIndex ) + 1 - 10;
				if( number < 1 )
					number = 1;
			}
			else
				number = World.Instance.Random.Next( maxIndex ) + 1;

			return animationName + ( number != 1 ? number.ToString() : "" );
		}

		void TickAnimations()
		{
			if( mainMeshObject == null || mainMeshObject.MeshObject == null )
				return;

			Vec3 mainBodyVelocity = GetGroundRelativeVelocity();

			MeshObject.AnimationState animationState = null;
			float animationVelocity = 1.0f;
			bool animationLoop = false;

			//find current animation
			MeshObject.AnimationState currentAnimationState = null;
			{
				foreach( MapObjectAttachedMesh.AnimationStateItem item in
					mainMeshObject.CurrentAnimationStates )
				{
					if( item.FadeOutBlendTime == 0 )
					{
						currentAnimationState = item.AnimationState;
						break;
					}
				}
			}

			//force set animation
			if( forceAnimationState != null )
			{
				if( currentAnimationState == forceAnimationState )
				{
					if( forceAnimationState.TimePosition + TickDelta * 2 >= forceAnimationState.Length )
						forceAnimationState = null;
					animationState = forceAnimationState;
				}
				else
					forceAnimationState = null;
			}

			//walk animation
			if( animationState == null )
			{
				if( IsOnGround() && mainBodyVelocity.ToVec2().LengthSqr() > .3f )
				{
					bool allowChange = true;

					if( currentAnimationState != null )
					{
						if( string.Compare( currentAnimationState.Name, 0,
							Type.WalkAnimationName, 0, Type.WalkAnimationName.Length ) == 0 )
						{
							if( currentAnimationState.TimePosition >= lastAnimationTimePosition )
								allowChange = false;
						}
					}

					if( allowChange )
					{
						string animationName = GetRandomAnimationNumber( Type.WalkAnimationName );
						animationState = mainMeshObject.MeshObject.GetAnimationState( animationName );
					}
					else
						animationState = currentAnimationState;

					animationLoop = true;

					animationVelocity = ( Rotation.GetInverse() * mainBodyVelocity ).X;
					animationVelocity *= Type.WalkAnimationVelocityMultiplier;
				}
			}

			//idle animation
			if( animationState == null )
			{
				bool allowChange = true;

				if( currentAnimationState != null )
				{
					if( string.Compare( currentAnimationState.Name, 0,
						Type.IdleAnimationName, 0, Type.IdleAnimationName.Length ) == 0 )
					{
						if( currentAnimationState.TimePosition >= lastAnimationTimePosition )
							allowChange = false;
					}
				}

				if( allowChange )
				{
					string animationName = GetRandomAnimationNumber( Type.IdleAnimationName );
					animationState = mainMeshObject.MeshObject.GetAnimationState( animationName );
				}
				else
					animationState = currentAnimationState;

				animationLoop = true;
				animationVelocity = 1.0f + World.Instance.Random.NextFloatCenter() * .1f;
			}

			mainMeshObject.ChangeCurrentAnimationState(
				animationState, animationVelocity, animationLoop, animationsBlendTime );

			//update lastAnimationTimePosition
			if( animationState != null )
				lastAnimationTimePosition = animationState.TimePosition;
			else
				lastAnimationTimePosition = 0;
		}

		protected override void OnSetForceAnimationState( string animationName )
		{
			base.OnSetForceAnimationState( animationName );

			if( mainMeshObject != null && mainMeshObject.MeshObject != null )
			{
				string numberedAnimationName = GetRandomAnimationNumber( animationName );
				forceAnimationState = mainMeshObject.MeshObject.GetAnimationState( numberedAnimationName );

				if( forceAnimationState != null )
				{
					//reset time for animation
					mainMeshObject.RemoveCurrentAnimationState( forceAnimationState, 0 );
					mainMeshObject.ChangeCurrentAnimationState( forceAnimationState,
						1, false, animationsBlendTime );
				}
			}
		}

		[Browsable( false )]
		public MeshObject.AnimationState ForceAnimationState
		{
			get { return forceAnimationState; }
		}

		protected override void OnJump()
		{
			base.OnJump();

			//jump animation
			if( mainMeshObject != null && mainMeshObject.MeshObject != null )
			{
				if( mainMeshObject.MeshObject.GetAnimationState( Type.JumpAnimationName ) != null )
					SetForceAnimationState( Type.JumpAnimationName );
			}
		}

	}
}
