// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Engine.EntitySystem;
using Engine.Renderer;
using Engine.MapSystem;
using Engine.MathEx;
using Engine.PhysicsSystem;
using GameCommon;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Corpse"/> entity type.
	/// </summary>
	public class CorpseType : DynamicType
	{
		[FieldSerialize]
		string deathAnimationName = "death";
		[FieldSerialize]
		string deadAnimationName = "dead";

		/// <summary>
		/// Gets or sets the name of animation when the object died. 
		/// The given animation is play after <see cref="DeadAnimationName"/>.
		/// </summary>
		[Description( "The name of animation when the object died. The given animation is play after \"DeadAnimationName\"." )]
		[DefaultValue( "death" )]
		public string DeathAnimationName
		{
			get { return deathAnimationName; }
			set { deathAnimationName = value; }
		}

		/// <summary>
		/// Gets or sets the name of animation when the object dies.
		/// </summary>
		[Description( "The name of animation when the object dies." )]
		[DefaultValue( "dead" )]
		public string DeadAnimationName
		{
			get { return deadAnimationName; }
			set { deadAnimationName = value; }
		}
	}

	/// <summary>
	/// Gives an opportunity to create corpses. A difference of a corpse from usual 
	/// object that he changes the orientation depending on a surface. 
	/// Also the class operates animations.
	/// </summary>
	public class Corpse : Dynamic
	{
		MapObjectAttachedMesh mainMeshObject;
		bool deadAnimation;
		int deathAnimationNumber;

		//

		CorpseType _type = null; public new CorpseType Type { get { return _type; } }

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );

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
			base.OnTick();

			if( PhysicsModel != null )
			{
				foreach( Body body in PhysicsModel.Bodies )
				{
					body.AngularVelocity = Vec3.Zero;

					Angles angles = Rotation.ToAngles();
					if( Math.Abs( angles.Roll ) > 30 || Math.Abs( angles.Pitch ) > 30 )
					{
						Quat oldRotation = body.OldRotation;
						body.Rotation = new Angles( 0, 0, angles.Yaw ).ToQuat();
						body.OldRotation = oldRotation;
					}
				}
			}

			if( mainMeshObject != null && mainMeshObject.MeshObject != null )
			{
				MeshObject.AnimationState animationState = null;
				float animationVelocity = 1.0f;
				bool animationLoop = false;

				if( !deadAnimation )
				{
					//Choose animation number: death, death2, death3
					if( deathAnimationNumber == 0 )
					{
						int maxAnimationNumber = 1;
						for( int number = 2; ; number++ )
						{
							if( mainMeshObject.MeshObject.GetAnimationState( 
								Type.DeathAnimationName + number.ToString() ) != null )
								maxAnimationNumber++;
							else
								break;
						}
						deathAnimationNumber = World.Instance.Random.Next( maxAnimationNumber ) + 1;
					}

					string animationName = Type.DeathAnimationName +
						( deathAnimationNumber != 1 ? deathAnimationNumber.ToString() : "" );

					animationState = mainMeshObject.MeshObject.GetAnimationState( animationName );

					if( animationState != null && animationState.TimePosition >= animationState.Length )
					{
						animationState = null;
						animationLoop = false;
						deadAnimation = true;
					}
				}

				if( animationState == null )
				{
					string animationName = Type.DeadAnimationName +
						( deathAnimationNumber != 1 ? deathAnimationNumber.ToString() : "" );

					animationState = mainMeshObject.MeshObject.GetAnimationState( animationName );
					animationLoop = true;
				}

				mainMeshObject.ChangeCurrentAnimationState(
					animationState, animationVelocity, animationLoop, 0 );
			}
		}
	}
}
