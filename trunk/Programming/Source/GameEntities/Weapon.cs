// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using Engine;
using Engine.MathEx;
using Engine.Utils;
using Engine.Renderer;
using Engine.EntitySystem;
using Engine.MapSystem;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Weapon"/> entity type.
	/// </summary>
	public abstract class WeaponType : DynamicType
	{
		public abstract class WeaponMode
		{
			[FieldSerialize]
			[DefaultValue( 0.0f )]
			float betweenFireTime = 0;

			[FieldSerialize]
			string soundFire;

			[FieldSerialize]
			[DefaultValue( typeof( Vec3 ), "0 0 0" )]
			Vec3 startOffsetPosition;

			[FieldSerialize]
			[DefaultValue( typeof( Quat ), "0 0 0 1" )]
			Quat startOffsetRotation = Quat.Identity;

			[FieldSerialize]
			[DefaultValue( typeof( Vec2 ), "0 0" )]
			Range useDistanceRange;

			[FieldSerialize]
			List<float> fireTimes = new List<float>();

			[FieldSerialize]
			string fireAnimationName = "fire";

			[FieldSerialize]
			string fireUnitAnimationName = "";

			[DefaultValue( 0.0f )]
			public float BetweenFireTime
			{
				get { return betweenFireTime; }
				set { betweenFireTime = value; }
			}

			[Editor( typeof( EditorSoundUITypeEditor ), typeof( UITypeEditor ) )]
			public string SoundFire
			{
				get { return soundFire; }
				set { soundFire = value; }
			}

			[DefaultValue( typeof( Vec3 ), "0 0 0" )]
			public Vec3 StartOffsetPosition
			{
				get { return startOffsetPosition; }
				set { startOffsetPosition = value; }
			}

			[DefaultValue( typeof( Quat ), "0 0 0" )]
			public Quat StartOffsetRotation
			{
				get { return startOffsetRotation; }
				set { startOffsetRotation = value; }
			}

			[DefaultValue( typeof( Vec2 ), "0 0" )]
			public Range UseDistanceRange
			{
				get { return useDistanceRange; }
				set { useDistanceRange = value; }
			}

			[Editor( typeof( FireTimesCollectionEditor ), typeof( UITypeEditor ) )]
			public List<float> FireTimes
			{
				get { return fireTimes; }
			}

			[DefaultValue( "fire" )]
			public string FireAnimationName
			{
				get { return fireAnimationName; }
				set { fireAnimationName = value; }
			}

			public string FireUnitAnimationName
			{
				get { return fireUnitAnimationName; }
				set { fireUnitAnimationName = value; }
			}

			[Browsable( false )]
			public abstract bool IsInitialized
			{
				get;
			}

			////////////////////////////////////////

			[EditorBrowsable( EditorBrowsableState.Never )]
			public class FireTimesCollectionEditor : PropertyGridUtils.ModalDialogCollectionEditor
			{
				public FireTimesCollectionEditor()
					: base( typeof( List<float> ) )
				{ }
			}

			////////////////////////////////////////

		}

		[FieldSerialize]
		string fpsMeshMaterialName = "";

		protected WeaponMode weaponNormalMode;
		protected WeaponMode weaponAlternativeMode;

		[FieldSerialize]
		string boneSlot = "";

		[FieldSerialize]
		[DefaultValue( "idle" )]
		string idleAnimationName = "idle";

		//

		[Editor( typeof( EditorMaterialUITypeEditor ), typeof( UITypeEditor ) )]
		public string FPSMeshMaterialName
		{
			get { return fpsMeshMaterialName; }
			set { fpsMeshMaterialName = value; }
		}

		[Browsable( false )]
		public WeaponMode WeaponNormalMode
		{
			get { return weaponNormalMode; }
		}

		[Browsable( false )]
		public WeaponMode WeaponAlternativeMode
		{
			get { return weaponAlternativeMode; }
		}

		public string BoneSlot
		{
			get { return boneSlot; }
			set { boneSlot = value; }
		}

		[DefaultValue( "idle" )]
		public string IdleAnimationName
		{
			get { return idleAnimationName; }
			set { idleAnimationName = value; }
		}

	}

	/// <summary>
	/// Defines the weapons. Both hand-held by characters or guns established on turret are weapons.
	/// </summary>
	public abstract class Weapon : Dynamic
	{
		MapObjectAttachedMesh mainMeshObject;
		bool fpsMeshMaterialNameEnabled;

		bool setForceFireRotation;
		Quat forceFireRotation;

		//for animations
		const float animationsBlendTime = .1f;
		//key: animation name; value: maximum index (walk, walk2, walk3)
		Dictionary<string, int> maxAnimationIndices = new Dictionary<string, int>();
		MeshObject.AnimationState forceAnimationState;
		float lastAnimationTimePosition;

		//

		WeaponType _type = null; public new WeaponType Type { get { return _type; } }

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

			if( fpsMeshMaterialNameEnabled && !string.IsNullOrEmpty( Type.FPSMeshMaterialName ) )
				UpdateMainMeshObjectMaterial();

			AddTimer();
		}

		protected override void OnTick()
		{
			base.OnTick();

			TickAnimations();
		}

		[Browsable( false )]
		abstract public bool Ready
		{
			get;
		}

		public delegate void PreFireDelegate( Weapon entity, bool alternative );
		public event PreFireDelegate PreFire;

		protected void DoPreFireEvent( bool alternative )
		{
			if( PreFire != null )
				PreFire( this, alternative );
		}

		public abstract bool TryFire( bool alternative );

		public void SetForceFireRotationLookTo( Vec3 lookTo )
		{
			setForceFireRotation = true;

			Quat rot;
			{
				Vec3 diff = lookTo - GetFirePosition( false );

				float dirh = MathFunctions.ATan( diff.Y, diff.X );
				float dirv = -MathFunctions.ATan( diff.Z, diff.ToVec2().LengthFast() );
				float halfdirh = dirh * .5f;
				rot = new Quat( new Vec3( 0, 0, MathFunctions.Sin( halfdirh ) ),
					MathFunctions.Cos( halfdirh ) );
				float halfdirv = dirv * .5f;
				rot *= new Quat( 0, MathFunctions.Sin( halfdirv ), 0, MathFunctions.Cos( halfdirv ) );
			}
			forceFireRotation = rot;
		}

		public void ResetForceFireRotationLookTo()
		{
			setForceFireRotation = false;
		}

		public abstract Quat GetFireRotation( bool alternative );

		protected Quat GetFireRotation( WeaponType.WeaponMode typeMode )
		{
			return ( setForceFireRotation ? forceFireRotation : Rotation ) * typeMode.StartOffsetRotation;
		}

		public abstract Vec3 GetFirePosition( bool alternative );

		protected Vec3 GetFirePosition( WeaponType.WeaponMode typeMode )
		{
			return Position + Rotation * typeMode.StartOffsetPosition;
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

			if( animationName == Type.IdleAnimationName )
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
		public bool FPSMeshMaterialNameEnabled
		{
			get { return fpsMeshMaterialNameEnabled; }
			set
			{
				if( fpsMeshMaterialNameEnabled == value )
					return;
				fpsMeshMaterialNameEnabled = value;

				if( !string.IsNullOrEmpty( Type.FPSMeshMaterialName ) )
					UpdateMainMeshObjectMaterial();
			}
		}

		void UpdateMainMeshObjectMaterial()
		{
			if( mainMeshObject == null )
				return;

			MeshObject meshObject = mainMeshObject.MeshObject;
			if( meshObject == null )
				return;

			//FPSMeshMaterialName
			if( fpsMeshMaterialNameEnabled && !string.IsNullOrEmpty( Type.FPSMeshMaterialName ) )
			{
				meshObject.SetMaterialNameForAllSubObjects( Type.FPSMeshMaterialName );
				return;
			}

			//default materials
			Mesh mesh = meshObject.Mesh;
			for( int n = 0; n < meshObject.SubObjects.Length; n++ )
			{
				if( n < mesh.SubMeshes.Count )
					meshObject.SubObjects[ n ].MaterialName = mesh.SubMeshes[ n ].MaterialName;
			}
		}
	}
}
