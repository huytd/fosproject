// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using Engine;
using Engine.EntitySystem;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.Renderer;
using GameCommon;
using Engine.UISystem;

namespace GameEntities
{
    public enum ItemSlot
    {
        None,
        MainWeapon,
        Pistol,
        Health,
        Ammo
    }

	/// <summary>
	/// Defines the <see cref="PlayerCharacter"/> entity type.
	/// </summary>
	public class PlayerCharacterType : GameCharacterType
	{
		[FieldSerialize]
		Vec3 weaponAttachPosition;

		[FieldSerialize]
		Vec3 weaponFPSAttachPosition;

		[FieldSerialize]
		List<WeaponItem> weapons = new List<WeaponItem>();

        [FieldSerialize]
        List<InventoryItem> items = new List<InventoryItem>();

        [FieldSerialize]
        int inventorySize;

    	///////////////////////////////////////////

		public class WeaponItem
		{
			[FieldSerialize]
			WeaponType weaponType;

			public WeaponType WeaponType
			{
				get { return weaponType; }
				set { weaponType = value; }
			}

			public override string ToString()
			{
				if( weaponType == null )
					return "(not initialized)";
				return weaponType.Name;
			}
		}

        public class InventoryItem
        {
            [FieldSerialize]
            DynamicType type;

            [FieldSerialize]
            ItemType itemType;

            [FieldSerialize]
            ItemSlot slot;

            [FieldSerialize]
            string icon;

            [FieldSerialize]
            string hint;

            int normalBulletCount;
            int normalMagazineCount;

            int alternativeBulletCount;
            int alternativeMagazineCount;

            public DynamicType Type
            {
                get { return type; }
                set { type = value; }
            }

            public ItemType ItemType
            {
                get { return itemType; }
                set { itemType = value; }
            }

            public ItemSlot Slot
            {
                get { return slot; }
                set { slot = value; }
            }

            [Editor(typeof(EditorTextureUITypeEditor), typeof(UITypeEditor))]
            public string Icon
            {
                get { return icon; }
                set { icon = value; }
            }

            public string Hint
            {
                get { return hint; }
                set { hint = value; }
            }

            [Browsable(false)]
            public int NormalBulletCount
            {
                get { return normalBulletCount; }
                set { normalBulletCount = value; }
            }

            [Browsable(false)]
            public int NormalMagazineCount
            {
                get { return normalMagazineCount; }
                set { normalMagazineCount = value; }
            }

            [Browsable(false)]
            public int AlternativeBulletCount
            {
                get { return alternativeBulletCount; }
                set { alternativeBulletCount = value; }
            }

            [Browsable(false)]
            public int AlternativeMagazineCount
            {
                get { return alternativeMagazineCount; }
                set { alternativeMagazineCount = value; }
            }

            public override string ToString()
            {
                if (type == null)
                    return "(not initialized)";
                return type.Name;
            }
        }

		///////////////////////////////////////////

		[DefaultValue( typeof( Vec3 ), "0 0 0" )]
		public Vec3 WeaponAttachPosition
		{
			get { return weaponAttachPosition; }
			set { weaponAttachPosition = value; }
		}

		[DefaultValue( typeof( Vec3 ), "0 0 0" )]
		public Vec3 WeaponFPSAttachPosition
		{
			get { return weaponFPSAttachPosition; }
			set { weaponFPSAttachPosition = value; }
		}

		public List<WeaponItem> Weapons
		{
			get { return weapons; }
		}

        public int InventorySize
        {
            get { return inventorySize; }
            set { inventorySize = value; }
        }

        

        public List<InventoryItem> Items
        {
            get { return items; }
            set { items = value; }
        }
	}

	public class PlayerCharacter : GameCharacter
	{
		[FieldSerialize]
		List<WeaponItem> weapons = new List<WeaponItem>();               

		[FieldSerialize]
		Weapon activeWeapon;
		MapObjectAttachedMapObject activeWeaponAttachedObject;

		bool allowContusionMotionBlur = true;
		
        [FieldSerialize]
		float contusionTimeRemaining;

		///////////////////////////////////////////

		public class WeaponItem
		{
			[FieldSerialize]
			internal bool exists;

			[FieldSerialize]
			internal int normalBulletCount;
			[FieldSerialize]
			internal int normalMagazineCount;

			[FieldSerialize]
			internal int alternativeBulletCount;
			[FieldSerialize]
			internal int alternativeMagazineCount;

			public bool Exists
			{
				get { return exists; }
			}

			public int NormalBulletCount
			{
				get { return normalBulletCount; }
			}

			public int NormalMagazineCount
			{
				get { return normalMagazineCount; }
			}

			public int AlternativeBulletCount
			{
				get { return alternativeBulletCount; }
			}

			public int AlternativeMagazineCount
			{
				get { return alternativeMagazineCount; }
			}
		}

		///////////////////////////////////////////

		public class ChangeMapInformation
		{
			public Vec3 position;
			public SphereDir lookDirection;
			public Vec3 velocity;

			public float life;

			public List<WeaponItem> weapons;
            public List<InventoryItem> items;

			public int activeWeaponIndex;
		}

		///////////////////////////////////////////

		PlayerCharacterType _type = null;

        public new PlayerCharacterType Type { get { return _type; } }       
           
        public class InventoryItem
        {
            int index = -1;
            int count = 0;

            public int Index
            {
                get { return index; }
                set { index = value; }
            }

            public int Count
            {
                get { return count; }
                set { count = value; }
            }

            public void Copy(InventoryItem Source)
            {
                index = Source.Index;
                count = Source.Count;
            }

            public static void Swap(InventoryItem Item1, InventoryItem Item2)
            {
                InventoryItem Temp = new InventoryItem();
                Temp.Copy(Item1);
                Item1.Copy(Item2);
                Item2.Copy(Temp);
            }
        }

        public new List<InventoryItem> items;

        [Browsable(false)]
        public new List<InventoryItem> Items
        {
            get { return items; }
            set { items = value; }
        }

		[Browsable( false )]
		public List<WeaponItem> Weapons
		{
			get { return weapons; }
			set { weapons = value; }
		}

		public int GetWeaponIndex( WeaponType weaponType )
		{
			for( int n = 0; n < Type.Weapons.Count; n++ )
				if( Type.Weapons[ n ].WeaponType == weaponType )
					return n;
			return -1;
		}

        InventoryItem mainWeapon = new InventoryItem();
        [Browsable( false )]
        public InventoryItem MainWeapon
         {
             get {return mainWeapon; }
             set { mainWeapon = value; }
         }

         int activePistol = -1;
         [Browsable( false )]
         public int ActivePistol
         {
             get { return activePistol; }
             set { activePistol = value; }
         }

 
         InventoryItem pistol = new InventoryItem();
         [Browsable( false )]
         public InventoryItem Pistol
         {
             get { return pistol; }
             set { pistol = value; }
         }

         
         int activeMainWeapon = -1;
         [Browsable( false )]
         public int ActiveMainWeapon
         {
             get { return activeMainWeapon; }
             set { activeMainWeapon = value; }
         }


         ItemSlot activeSlot;
         [Browsable(false)]
         public ItemSlot ActiveSlot
         {
             get { return activeSlot; }
             set { activeSlot = value; }
         }


         [Browsable(false)]
         public int InventorySize
         {
             get { return Type.InventorySize; }
         }


         Item useItem;
         [Browsable(false)]
         public Item UseItem
         {
             get { return useItem; }
             set { useItem = value; }
         }

         EInventory inventory;
         [Browsable(false)]
         public EInventory Inventory
         {
             get { return inventory; }
             set { inventory = value; }
         }


         public int GetFreeSlot()
         {
             for (int i = 0; i < Type.InventorySize; i++)
             {
                 if (items[i].Index == -1)
                 {
                     return i;
                 }
             }
             return -1;
         }

         public int GetBulletSlot(int bulletIndex)
         {
             for (int i = 0; i < InventorySize; i++)
             {
                 if (items[i].Index == bulletIndex)
                 {
                     return i;
                 }
             }
             return -1;
         }

         public int GetBulletGun(int bulletIndex)
         {
             for (int i = 0; i < Type.Items.Count; i++)
             {
                 GunType gunType = Type.Items[i].Type as GunType;
                 if (gunType != null)
                 {
                     if (gunType.NormalMode.BulletType == Type.Items[bulletIndex].Type ||
                     gunType.AlternativeMode.BulletType == Type.Items[bulletIndex].Type)
                     {
                         return i;
                     }
                 }
             }
             return -1;
         }

         public bool SetAmmoCount(BulletType bulletType, int count)
         {
             int index = GetItemIndex(bulletType);
             if (index != -1)
             {
                 int slot = GetBulletSlot(index);
                 if (slot == -1)
                 {
                     slot = GetFreeSlot();
                     if (slot != -1)
                     {
                         items[slot].Index = index;
                     }
                     else
                     {
                         return false;
                     }
                 }
                 items[slot].Count = count;
             }
             return true;
         }

	

        public int GetItemIndex(DynamicType itemType)
        {
            for (int i = 0; i < Type.Items.Count; i++)
            {
                if (Type.Items[i].Type == itemType)
                {
                    return i;
                }
            }
            return -1;
        }


        public bool TakeWeapon(WeaponType weaponType)
        {
            int index = GetWeaponIndex(weaponType);
            if (index == -1)
                return false;

            if (weapons[index].exists)
                return true;

            weapons[index].exists = true;

            SetActiveWeapon(index);

            return true;
        }


        public bool TakeItem(DynamicType itemType)
        {
            int index = GetItemIndex(itemType);
            if (index == -1)
                return false;

            int Slot = GetFreeSlot();
            if (Slot != -1)
            {
                items[Slot].Index = index;
                return true;
            }
            return false;
        }



        public bool TakeBullets(BulletType bulletType, int count)
        {
            bool taked = false;

            for (int i = 0; i < Type.Items.Count;  i++)
            {
                GunType gunType = Type.Items[i].Type as GunType;
                if (gunType == null)
                    continue;

                if (gunType.NormalMode.BulletType == bulletType)
                {
                    if (Type.Items[i].NormalBulletCount < gunType.NormalMode.BulletCapacity)
                    {
                        int newCount = Type.Items[i].NormalBulletCount + count;
                        if (newCount > gunType.NormalMode.BulletCapacity)
                        {
                            newCount = gunType.NormalMode.BulletCapacity;
                        }
                        taked = SetAmmoCount(bulletType, newCount);
                        if (taked)
                        {
                            Type.Items[i].NormalBulletCount = newCount;
                        }
                    }
                }
                if (gunType.AlternativeMode.BulletType == bulletType)
                {
                    if (Type.Items[i].AlternativeBulletCount < gunType.AlternativeMode.BulletCapacity)
                    {
                        int newCount = Type.Items[i].AlternativeBulletCount + count;
                        if (newCount > gunType.AlternativeMode.BulletCapacity)
                        {
                            newCount = gunType.AlternativeMode.BulletCapacity;
                        }
                        taked = SetAmmoCount(bulletType, newCount);
                        if (taked)
                        {
                            Type.Items[i].AlternativeBulletCount = newCount;
                        }
                    }
                }
            }
            if (ActiveWeapon != null)
            {
                Gun activeGun = activeWeapon as Gun;
                if (activeGun != null)
                {
                    activeGun.AddBullets(bulletType, count);
                    if (activeGun.Type.NormalMode.BulletType == bulletType)
                    {
                        SetAmmoCount(bulletType, activeGun.NormalMode.BulletCount);
                    }
                    else if (activeGun.Type.AlternativeMode.BulletType == bulletType)
                    {
                        SetAmmoCount(bulletType, activeGun.AlternativeMode.BulletCount);
                    }
                }
            }
            return taked;
        }




        public bool SetActiveWeapon(int index)
        {
            if (index < -1 || index >= items.Count) return false;
            if (activeWeapon != null)
            {
                activeWeapon.PreFire -= activeWeapon_PreFire;
                if (index != -1 && Type.Items[index].Type.Name == activeWeapon.Type.Name)
                    return true;
                foreach (MapObjectAttachedObject attachedObject in AttachedObjects)
                {
                    MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
                    if (attachedMapObject == null)
                        continue;
                    Weapon weapon = attachedMapObject.MapObject as Weapon;
                    if (weapon == activeWeapon)
                    {
                        Gun activeGun = activeWeapon as Gun;
                        if (activeGun != null)
                        {
                            int activeIndex = GetActiveWeapon();
                            Type.Items[activeIndex].NormalMagazineCount = activeGun.NormalMode.BulletMagazineCount;
                            Type.Items[activeIndex].AlternativeMagazineCount = activeGun.AlternativeMode.BulletMagazineCount;
                        }

                        Detach(attachedMapObject);
                        weapon.SetShouldDelete();
                        activeWeapon = null;
                        activeWeaponAttachedObject = null;
                        break;
                    }
                }
            }

            if (index != -1)
            {
                activeWeapon = (Weapon)Entities.Instance.Create(Type.Items[index].Type, Parent);

                Gun activeGun = activeWeapon as Gun;

                if (activeGun != null)
                {
                    activeGun.NormalMode.BulletCount = Type.Items[index].NormalBulletCount;
                    activeGun.NormalMode.BulletMagazineCount = Type.Items[index].NormalMagazineCount;

                    activeGun.AlternativeMode.BulletCount = Type.Items[index].AlternativeBulletCount;
                    activeGun.AlternativeMode.BulletMagazineCount = Type.Items[index].AlternativeMagazineCount;
                }

                activeWeapon.PostCreate();

                activeWeaponAttachedObject = new MapObjectAttachedMapObject();
                activeWeaponAttachedObject.MapObject = activeWeapon;
                activeWeaponAttachedObject.BoneSlot = GetBoneSlotFromAttachedMeshes(activeWeapon.Type.BoneSlot);
                if (activeWeaponAttachedObject.BoneSlot == null)
                {
                    activeWeaponAttachedObject.PositionOffset = Type.WeaponAttachPosition;
                }
                Attach(activeWeaponAttachedObject);

                activeWeapon.PreFire += activeWeapon_PreFire;

                SoundPlay3D(Type.Items[index].ItemType.SoundTake, .5f, true);

                WeaponTryReload();
            }

            return true;
        }
 

		void CreateActiveWeaponAttachedObject()
		{
			activeWeaponAttachedObject = new MapObjectAttachedMapObject();
			activeWeaponAttachedObject.MapObject = activeWeapon;
			activeWeaponAttachedObject.BoneSlot = GetBoneSlotFromAttachedMeshes(
				activeWeapon.Type.BoneSlot );
			if( activeWeaponAttachedObject.BoneSlot == null )
				activeWeaponAttachedObject.PositionOffset = Type.WeaponAttachPosition;
			Attach( activeWeaponAttachedObject );
		}


        int GetActiveWeapon()
        {
            if (activeSlot == ItemSlot.MainWeapon)
            {
                return activeMainWeapon;
            }
            else if (activeSlot == ItemSlot.Pistol)
            {
                return activePistol;
            }
            return -1;
        }
 

		void SetActiveNextWeapon()
		{
			if( Weapons.Count == 0 )
				return;

			int index = GetActiveWeapon();
			int counter = Weapons.Count;
			while( counter != 0 )
			{
				counter--;
				index++;
				if( index >= Weapons.Count )
					index = 0;
				if( !Weapons[ index ].Exists )
					continue;

				SetActiveWeapon( index );
				break;
			}
		}

		void SetActivePreviousWeapon()
		{
			if( Weapons.Count == 0 )
				return;

			int index = GetActiveWeapon();
			int counter = Weapons.Count;
			while( counter != 0 )
			{
				counter--;
				index--;
				if( index < 0 )
					index = Weapons.Count - 1;
				if( !Weapons[ index ].Exists )
					continue;

				SetActiveWeapon( index );
				break;
			}
		}

		protected override void OnPreCreate( bool loaded )
		{
			base.OnPreCreate( loaded );

			if( !loaded )
			{
				for( int n = 0; n < Type.Weapons.Count; n++ )
					weapons.Add( new WeaponItem() );
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate( bool loaded )
		{
			base.OnPostCreate( loaded );
			AddTimer();

			if( loaded && EntitySystemWorld.Instance.SerializationMode == SerializationModes.World )
			{
				if( activeWeapon != null )
				{
					activeWeapon.PreFire += activeWeapon_PreFire;

					if( activeWeaponAttachedObject == null )
						CreateActiveWeaponAttachedObject();
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			if( contusionTimeRemaining != 0 && AllowContusionMotionBlur )
				RendererWorld.Instance.DefaultViewport.SetCompositorEnabled( "MotionBlur", false );

			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
		protected override void OnRelatedEntityDelete( Entity entity )
		{
			base.OnRelatedEntityDelete( entity );

			if( activeWeapon == entity )
			{
				activeWeapon.PreFire -= new Weapon.PreFireDelegate( activeWeapon_PreFire );
				activeWeapon = null;
				activeWeaponAttachedObject = null;
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			if( Intellect != null )
			{
				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire1 ) )
					WeaponTryFire( false );

				if( Intellect.IsControlKeyPressed( GameControlKeys.Fire2 ) )
					WeaponTryFire( true );
			}

			TickContusionMotionBlur();

			if( activeWeapon == null || activeWeapon.Ready )
				UpdateTPSArcadeRotation();
		}

		void activeWeapon_PreFire( Weapon entity, bool alternative )
		{
			UpdateTPSArcadeRotation();
		}

		void UpdateTPSArcadeRotation()
		{
			//TPS arcade specific (camera observe)
			//Update rotation
			if( GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade &&
				LastTickForceVector != Vec2.Zero )
			{
				if( PlayerIntellect.Instance != null &&
					PlayerIntellect.Instance.ControlledObject == this &&
					!PlayerIntellect.Instance.FPSCamera )
				{
					SetTurnToPosition( Position +
						new Vec3( LastTickForceVector.X, LastTickForceVector.Y, 0 ) * 100 );
				}
			}
		}

		void TickContusionMotionBlur()
		{
			float blur = 0;
			if( contusionTimeRemaining != 0 )
			{
				contusionTimeRemaining -= TickDelta;
				if( contusionTimeRemaining < 0 )
					contusionTimeRemaining = 0;

				if( Intellect != null && ( (PlayerIntellect)Intellect ).FPSCamera )
				{
					blur = contusionTimeRemaining;
					if( blur > .8f )
						blur = .8f;
				}
			}

			if( AllowContusionMotionBlur )
			{
				MotionBlurCompositorInstance instance = (MotionBlurCompositorInstance)
					RendererWorld.Instance.DefaultViewport.GetCompositorInstance( "MotionBlur" );
				if( instance != null )
				{
					instance.Enabled = blur != 0;
					instance.Blur = blur;
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnRender(Camera)"/>.</summary>
		protected override void OnRender( Camera camera )
		{
			base.OnRender( camera );

			//no update in cubemap generation mode
			if( Map.Instance.CubemapGenerationMode )
				return;

			bool playerIntellectFPSCamera = false;
			{
				if( Intellect != null && Intellect == PlayerIntellect.Instance )
					playerIntellectFPSCamera = PlayerIntellect.Instance.FPSCamera;
			}
			bool fpsCamera = playerIntellectFPSCamera && RendererWorld.Instance.DefaultCamera == camera;

			if( activeWeapon != null && GameMap.Instance.GameType != GameMap.GameTypes.TPSArcade )
			{
				//FPS mesh material
				activeWeapon.FPSMeshMaterialNameEnabled = 
					fpsCamera && !camera.IsForShadowMapGeneration();

				//update weapon vertical orientation
				if( activeWeaponAttachedObject.MapObject is Gun )
				{
					//for guns
					if( fpsCamera )
					{
						Vec3 diff = PlayerIntellect.Instance.LookDirection.GetVector();

						float dirV = -MathFunctions.ATan( diff.Z, diff.ToVec2().Length() );
						float halfDirV = dirV * .5f;
						Quat rot = new Quat( 0, MathFunctions.Sin( halfDirV ), 0,
							MathFunctions.Cos( halfDirV ) );

						activeWeaponAttachedObject.RotationOffset = rot;
						activeWeaponAttachedObject.PositionOffset =
							Type.FPSCameraOffset + Type.WeaponFPSAttachPosition * rot;
					}
					else
					{
						Vec3 diff = SeePosition - Position;

						float dirV = -MathFunctions.ATan( diff.Z, diff.ToVec2().Length() );
						float halfDirV = dirV * .5f;
						Quat rot = new Quat( 0, MathFunctions.Sin( halfDirV ), 0,
							MathFunctions.Cos( halfDirV ) );

						activeWeaponAttachedObject.RotationOffset = rot;
						activeWeaponAttachedObject.PositionOffset = Type.WeaponAttachPosition;
					}
				}
				else
				{
					//for melee weapons
					activeWeaponAttachedObject.RotationOffset = Quat.Identity;
					activeWeaponAttachedObject.PositionOffset = Vec3.Zero;
				}

				//Update transform of the attached weapon.
				//That there was no twitching because of interpolation
				{
					Vec3 p;
					Quat r;
					Vec3 s;
					activeWeaponAttachedObject.GetGlobalTransform( out p, out r, out s );
					activeWeaponAttachedObject.MapObject.SetTransform( p, r, s );
					activeWeaponAttachedObject.GetGlobalOldTransform( out p, out r, out s );
					activeWeaponAttachedObject.MapObject.SetOldTransform( p, r, s );
				}

				//no cast shadows from active weapon in the FPS mode
				foreach( MapObjectAttachedObject weaponAttachedObject in activeWeapon.AttachedObjects )
				{
					MapObjectAttachedMesh weaponAttachedMesh = weaponAttachedObject as MapObjectAttachedMesh;
					if( weaponAttachedMesh != null && weaponAttachedMesh.MeshObject != null )
					{
						if( weaponAttachedMesh.RemainingTime == 0 )
							weaponAttachedMesh.MeshObject.CastShadows = !fpsCamera;
					}
				}
			}

			//only weapon visible in the FPS mode
			foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
				attachedObject.Visible = !fpsCamera || attachedObject == activeWeaponAttachedObject;

			//no cast shadows in the FPS mode
			if( camera.IsForShadowMapGeneration() && playerIntellectFPSCamera )
			{
				foreach( MapObjectAttachedObject attachedObject in AttachedObjects )
					attachedObject.Visible = false;
			}
		}


        protected override void OnIntellectCommand(Intellect.Command command)
        {
            base.OnIntellectCommand(command);

            if (command.KeyPressed)
            {
                if (command.Key == GameControlKeys.MainWeapon)
                {
                    SetActiveWeapon(ActiveMainWeapon);
                    ActiveSlot = ItemSlot.MainWeapon;
                }
                else if (command.Key == GameControlKeys.Pistol)
                {
                    SetActiveWeapon(ActivePistol);
                    ActiveSlot = ItemSlot.Pistol;
                }
                else if (command.Key == GameControlKeys.Fire1)
                {
                    WeaponTryFire(false);
                }
                else if (command.Key == GameControlKeys.Fire2)
                {
                    WeaponTryFire(true);
                }
                else if (command.Key == GameControlKeys.Reload)
                {
                    WeaponTryReload();
                }
                else if (command.Key == GameControlKeys.Use)
                {                    
                }
                else if (command.Key == GameControlKeys.Inventory)
                {
                    if (inventory != null)
                    {
                        inventory.SetShouldDetach();
                        inventory = null;

                        EntitySystemWorld.Instance.Simulation = true;
                        EngineApp.Instance.MouseRelativeMode = true;
                    }
                    else
                    {
                        inventory = new EInventory();
                        ScreenControlManager.Instance.Controls.Add(inventory);

                        EntitySystemWorld.Instance.Simulation = false;
                        EngineApp.Instance.MouseRelativeMode = false;
                    }
                }
            }
        }
 
 

		[Browsable( false )]
		public Weapon ActiveWeapon
		{
			get { return activeWeapon; }
		}
 void WeaponTryFire( bool alternative )
 {
     if( activeWeapon == null )
     return;
 
     //set real weapon fire direction
     {
     Vec3 seeDir = SeePosition - RendererWorld.Instance.DefaultCamera.Position;
 
     Vec3 lookTo = SeePosition;
 
     for( int iter = 0; iter < 100; iter++ )
     {
         activeWeapon.SetForceFireRotationLookTo( lookTo );
         Vec3 fireDir = activeWeapon.GetFireRotation( alternative ) * new Vec3( 1, 0, 0 );
         Degree angle = MathUtils.GetVectorsAngle( seeDir, fireDir );
 
         if( angle < 80 )
             break;
 
         const float step = .3f;
         lookTo += seeDir * step;
         }
 
         activeWeapon.SetForceFireRotationLookTo( lookTo );
     }
 
     bool fired = activeWeapon.TryFire( alternative );
 
     Gun activeGun = activeWeapon as Gun;
     if( activeGun != null )     {
         if( fired )
         {
             int index = GetItemIndex( activeWeapon.Type );
             Type.Items[ index ].NormalBulletCount = activeGun.NormalMode.BulletCount;
             Type.Items[ index ].NormalMagazineCount = activeGun.NormalMode.BulletMagazineCount;
             Type.Items[ index ].AlternativeBulletCount = activeGun.AlternativeMode.BulletCount;
             Type.Items[ index ].AlternativeMagazineCount = activeGun.AlternativeMode.BulletMagazineCount;
 
             int itemIndex = GetItemIndex( activeGun.Type.NormalMode.BulletType );
             if( index != -1 )
             {
                 int slot = GetBulletSlot( itemIndex );
                 if( slot != -1 )                 {
                     items[ slot ].Count = activeGun.NormalMode.BulletCount;
                 }
             }
         }
     }
 }
 

		void WeaponTryReload()
		{
			if( activeWeapon == null )
				return;

			Gun activeGun = activeWeapon as Gun;
			if( activeGun != null )
				activeGun.TryReload();
		}

		public ChangeMapInformation GetChangeMapInformation( MapChangeRegion region )
		{
			ChangeMapInformation information = new ChangeMapInformation();

			information.position = ( OldPosition - region.Position ) * region.Rotation.GetInverse();
			information.lookDirection = ( (PlayerIntellect)Intellect ).LookDirection *
				region.Rotation.GetInverse();
			information.velocity = MainBody.LinearVelocity * region.Rotation.GetInverse();

			information.life = Life;

			information.weapons = weapons;
			information.activeWeaponIndex = GetWeaponIndex(
				( activeWeapon != null ) ? activeWeapon.Type : null );

			return information;
		}

		public void ApplyChangeMapInformation( ChangeMapInformation information,
			SpawnPoint spawnPoint )
		{
			if( spawnPoint == null )
				return;

			Position = spawnPoint.Position + information.position * spawnPoint.Rotation;
			( (PlayerIntellect)Intellect ).LookDirection =
				information.lookDirection * spawnPoint.Rotation;
			MainBody.LinearVelocity = information.velocity * spawnPoint.Rotation;

			OldPosition = Position;
			OldRotation = Rotation;

			weapons = information.weapons;
			SetActiveWeapon( information.activeWeaponIndex );

			Life = information.life;
		}

		public float ContusionTimeRemaining
		{
			get { return contusionTimeRemaining; }
			set { contusionTimeRemaining = value; }
		}

		public bool AllowContusionMotionBlur
		{
			get { return allowContusionMotionBlur; }
			set { allowContusionMotionBlur = value; }
		}

		protected override void OnCopyTransform( MapObject from )
		{
			base.OnCopyTransform( from );
			SetTurnToPosition( from.Position + from.Rotation * new Vec3( 100, 0, 0 ) );
		}

	}
}
