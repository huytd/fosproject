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

namespace GameEntities
{
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
                if (weaponType == null)
                    return "(not initialized)";
                return weaponType.Name;
            }
        }

        ///////////////////////////////////////////

        [DefaultValue(typeof(Vec3), "0 0 0")]
        public Vec3 WeaponAttachPosition
        {
            get { return weaponAttachPosition; }
            set { weaponAttachPosition = value; }
        }

        [DefaultValue(typeof(Vec3), "0 0 0")]
        public Vec3 WeaponFPSAttachPosition
        {
            get { return weaponFPSAttachPosition; }
            set { weaponFPSAttachPosition = value; }
        }

        public List<WeaponItem> Weapons
        {
            get { return weapons; }
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
            public int activeWeaponIndex;
        }

        ///////////////////////////////////////////

        PlayerCharacterType _type = null; public new PlayerCharacterType Type { get { return _type; } }

        [Browsable(false)]
        public List<WeaponItem> Weapons
        {
            get { return weapons; }
            set { weapons = value; }
        }

        public int GetWeaponIndex(WeaponType weaponType)
        {
            for (int n = 0; n < Type.Weapons.Count; n++)
                if (Type.Weapons[n].WeaponType == weaponType)
                    return n;
            return -1;
        }

        public bool TakeWeapon(WeaponType weaponType)
        {
            int index = GetWeaponIndex(weaponType);
            if (index == -1)
                return false;

            if (weapons[index].exists)
                SetActiveWeapon(index);    

            weapons[index].exists = true;

            SetActiveWeapon(index);

            return true;
        }

        public bool TakeBullets(BulletType bulletType, int count)
        {
            bool taked = false;

            for (int n = 0; n < weapons.Count; n++)
            {
                GunType gunType = Type.Weapons[n].WeaponType as GunType;
                if (gunType == null)
                    continue;

                if (gunType.NormalMode.BulletType == bulletType)
                {
                    if (weapons[n].normalBulletCount < gunType.NormalMode.BulletCapacity)
                    {
                        taked = true;
                        weapons[n].normalBulletCount += count;
                        if (weapons[n].normalBulletCount > gunType.NormalMode.BulletCapacity)
                            weapons[n].normalBulletCount = gunType.NormalMode.BulletCapacity;
                    }
                }
                if (gunType.AlternativeMode.BulletType == bulletType)
                {
                    if (weapons[n].alternativeBulletCount < gunType.AlternativeMode.BulletCapacity)
                    {
                        taked = true;
                        weapons[n].alternativeBulletCount += count;
                        if (weapons[n].alternativeBulletCount > gunType.AlternativeMode.BulletCapacity)
                            weapons[n].alternativeBulletCount = gunType.AlternativeMode.BulletCapacity;
                    }
                }
            }

            if (ActiveWeapon != null)
            {
                Gun activeGun = activeWeapon as Gun;
                if (activeGun != null)
                    activeGun.AddBullets(bulletType, count);
            }

            return taked;
        }

        public bool RemoveAllWeapon()
        {

            foreach (MapObjectAttachedObject attachedObject in AttachedObjects)
            {
                MapObjectAttachedMapObject attachedMapObject = attachedObject as MapObjectAttachedMapObject;
                if (attachedMapObject == null)
                    continue;

                Weapon weapon = attachedMapObject.MapObject as Weapon;
                if (weapon == activeWeapon)
                {
                    Gun activeGun = activeWeapon as Gun;                    
                    Detach(attachedMapObject);
                    //weapon.SetShouldDelete();
                    activeWeapon = null;
                    activeWeaponAttachedObject = null;
                    break;
                }
            }

            
            return true;
        }

        public bool SetActiveWeapon(int index)
        {
            if (index < -1 || index >= weapons.Count)
                return false;

            if (index != -1)
                if (!weapons[index].exists)
                    return false;

            if (activeWeapon != null)
            {
                activeWeapon.PreFire -= activeWeapon_PreFire;

                if (index != -1 && Type.Weapons[index].WeaponType == activeWeapon.Type)
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
                            int activeIndex = GetWeaponIndex(activeWeapon.Type);
                            weapons[activeIndex].normalMagazineCount =
                                activeGun.NormalMode.BulletMagazineCount;
                            weapons[activeIndex].alternativeMagazineCount =
                                activeGun.AlternativeMode.BulletMagazineCount;
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
                activeWeapon = (Weapon)Entities.Instance.Create(
                    Type.Weapons[index].WeaponType, Parent);

                Gun activeGun = activeWeapon as Gun;

                if (activeGun != null)
                {
                    activeGun.NormalMode.BulletCount = weapons[index].NormalBulletCount;
                    activeGun.NormalMode.BulletMagazineCount = weapons[index].NormalMagazineCount;

                    activeGun.AlternativeMode.BulletCount = weapons[index].AlternativeBulletCount;
                    activeGun.AlternativeMode.BulletMagazineCount =
                        weapons[index].AlternativeMagazineCount;
                }

                activeWeapon.PostCreate();

                CreateActiveWeaponAttachedObject();

                activeWeapon.PreFire += activeWeapon_PreFire;
            }

            return true;
        }

        void CreateActiveWeaponAttachedObject()
        {
            activeWeaponAttachedObject = new MapObjectAttachedMapObject();
            activeWeaponAttachedObject.MapObject = activeWeapon;
            activeWeaponAttachedObject.BoneSlot = GetBoneSlotFromAttachedMeshes(
                activeWeapon.Type.BoneSlot);
            if (activeWeaponAttachedObject.BoneSlot == null)
                activeWeaponAttachedObject.PositionOffset = Type.WeaponAttachPosition;
            Attach(activeWeaponAttachedObject);
        }

        int GetActiveWeapon()
        {
            if (activeWeapon == null)
                return -1;
            for (int n = 0; n < Weapons.Count; n++)
            {
                if (weapons[n].Exists)
                {
                    if (Type.Weapons[n].WeaponType == activeWeapon.Type)
                        return n;
                }
            }
            return -1;
        }

        void SetActiveNextWeapon()
        {
            if (Weapons.Count == 0)
                return;

            int index = GetActiveWeapon();
            int counter = Weapons.Count;
            while (counter != 0)
            {
                counter--;
                index++;
                if (index >= Weapons.Count)
                    index = 0;
                if (!Weapons[index].Exists)
                    continue;

                SetActiveWeapon(index);
                break;
            }
        }

        void SetActivePreviousWeapon()
        {
            if (Weapons.Count == 0)
                return;

            int index = GetActiveWeapon();
            int counter = Weapons.Count;
            while (counter != 0)
            {
                counter--;
                index--;
                if (index < 0)
                    index = Weapons.Count - 1;
                if (!Weapons[index].Exists)
                    continue;

                SetActiveWeapon(index);
                break;
            }
        }

        protected override void OnPreCreate(bool loaded)
        {
            base.OnPreCreate(loaded);

            if (!loaded)
            {
                for (int n = 0; n < Type.Weapons.Count; n++)
                    weapons.Add(new WeaponItem());
            }
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
        protected override void OnPostCreate(bool loaded)
        {
            base.OnPostCreate(loaded);
            AddTimer();

            if (loaded && EntitySystemWorld.Instance.SerializationMode == SerializationModes.World)
            {
                if (activeWeapon != null)
                {
                    activeWeapon.PreFire += activeWeapon_PreFire;

                    if (activeWeaponAttachedObject == null)
                        CreateActiveWeaponAttachedObject();
                }
            }
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
        protected override void OnDestroy()
        {
            if (contusionTimeRemaining != 0 && AllowContusionMotionBlur)
                RendererWorld.Instance.DefaultViewport.SetCompositorEnabled("MotionBlur", false);


            base.OnDestroy();
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnRelatedEntityDelete(Entity)"/></summary>
        protected override void OnRelatedEntityDelete(Entity entity)
        {
            base.OnRelatedEntityDelete(entity);

            if (activeWeapon == entity)
            {
                activeWeapon.PreFire -= new Weapon.PreFireDelegate(activeWeapon_PreFire);
                activeWeapon = null;
                activeWeaponAttachedObject = null;
            }
        }

        /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
        protected override void OnTick()
        {
            base.OnTick();

            if (Intellect != null)
            {
                if (Intellect.IsControlKeyPressed(GameControlKeys.Fire1))
                    WeaponTryFire(false);

                if (Intellect.IsControlKeyPressed(GameControlKeys.Fire2))
                    WeaponTryFire(true);
            }

            TickContusionMotionBlur();

            if (activeWeapon == null || activeWeapon.Ready)
                UpdateTPSArcadeRotation();
        }

        void activeWeapon_PreFire(Weapon entity, bool alternative)
        {
            UpdateTPSArcadeRotation();
        }

        void UpdateTPSArcadeRotation()
        {
            //TPS arcade specific (camera observe)
            //Update rotation
            if (GameMap.Instance.GameType == GameMap.GameTypes.TPSArcade &&
                LastTickForceVector != Vec2.Zero)
            {
                if (PlayerIntellect.Instance != null &&
                    PlayerIntellect.Instance.ControlledObject == this &&
                    !PlayerIntellect.Instance.FPSCamera)
                {
                    SetTurnToPosition(Position +
                        new Vec3(LastTickForceVector.X, LastTickForceVector.Y, 0) * 100);
                }
            }
        }

        void TickContusionMotionBlur()
        {
            float blur = 0;
            if (contusionTimeRemaining != 0)
            {
                contusionTimeRemaining -= TickDelta;
                if (contusionTimeRemaining < 0)
                    contusionTimeRemaining = 0;

                if (Intellect != null && ((PlayerIntellect)Intellect).FPSCamera)
                {
                    blur = contusionTimeRemaining;
                    if (blur > .8f)
                        blur = .8f;
                }
            }

            if (AllowContusionMotionBlur)
            {
                MotionBlurCompositorInstance instance = (MotionBlurCompositorInstance)
                    RendererWorld.Instance.DefaultViewport.GetCompositorInstance("MotionBlur");
                if (instance != null)
                {
                    instance.Enabled = blur != 0;
                    instance.Blur = blur;
                }
            }
        }

        /// <summary>Overridden from <see cref="Engine.MapSystem.MapObject.OnRender(Camera)"/>.</summary>
        protected override void OnRender(Camera camera)
        {
            base.OnRender(camera);

            //no update in cubemap generation mode
            if (Map.Instance.CubemapGenerationMode)
                return;

            bool playerIntellectFPSCamera = false;
            {
                if (Intellect != null && Intellect == PlayerIntellect.Instance)
                    playerIntellectFPSCamera = PlayerIntellect.Instance.FPSCamera;
            }
            bool fpsCamera = playerIntellectFPSCamera && RendererWorld.Instance.DefaultCamera == camera;

            if (activeWeapon != null && GameMap.Instance.GameType != GameMap.GameTypes.TPSArcade)
            {
                //FPS mesh material
                activeWeapon.FPSMeshMaterialNameEnabled =
                    fpsCamera && !camera.IsForShadowMapGeneration();

                //update weapon vertical orientation
                if (activeWeaponAttachedObject.MapObject is Gun)
                {
                    //for guns
                    if (fpsCamera)
                    {
                        Vec3 diff = PlayerIntellect.Instance.LookDirection.GetVector();

                        float dirV = -MathFunctions.ATan(diff.Z, diff.ToVec2().Length());
                        float halfDirV = dirV * .5f;
                        Quat rot = new Quat(0, MathFunctions.Sin(halfDirV), 0,
                            MathFunctions.Cos(halfDirV));

                        activeWeaponAttachedObject.RotationOffset = rot;
                        activeWeaponAttachedObject.PositionOffset =
                            Type.FPSCameraOffset + Type.WeaponFPSAttachPosition * rot;
                    }
                    else
                    {
                        Vec3 diff = SeePosition - Position;

                        float dirV = -MathFunctions.ATan(diff.Z, diff.ToVec2().Length());
                        float halfDirV = dirV * .5f;
                        Quat rot = new Quat(0, MathFunctions.Sin(halfDirV), 0,
                            MathFunctions.Cos(halfDirV));

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
                    activeWeaponAttachedObject.GetGlobalTransform(out p, out r, out s);
                    activeWeaponAttachedObject.MapObject.SetTransform(p, r, s);
                    activeWeaponAttachedObject.GetGlobalOldTransform(out p, out r, out s);
                    activeWeaponAttachedObject.MapObject.SetOldTransform(p, r, s);
                }

                //no cast shadows from active weapon in the FPS mode
                foreach (MapObjectAttachedObject weaponAttachedObject in activeWeapon.AttachedObjects)
                {
                    MapObjectAttachedMesh weaponAttachedMesh = weaponAttachedObject as MapObjectAttachedMesh;
                    if (weaponAttachedMesh != null && weaponAttachedMesh.MeshObject != null)
                    {
                        if (weaponAttachedMesh.RemainingTime == 0)
                            weaponAttachedMesh.MeshObject.CastShadows = !fpsCamera;
                    }
                }
            }

            //only weapon visible in the FPS mode
            foreach (MapObjectAttachedObject attachedObject in AttachedObjects)
                attachedObject.Visible = !fpsCamera || attachedObject == activeWeaponAttachedObject;

            //no cast shadows in the FPS mode
            if (camera.IsForShadowMapGeneration() && playerIntellectFPSCamera)
            {
                foreach (MapObjectAttachedObject attachedObject in AttachedObjects)
                    attachedObject.Visible = false;
            }
        }

        protected override void OnIntellectCommand(Intellect.Command command)
        {
            base.OnIntellectCommand(command);

            if (command.KeyPressed)
            {
                if (command.Key >= GameControlKeys.Weapon1 && command.Key <= GameControlKeys.Weapon9)
                {
                    int index = (int)command.Key - (int)GameControlKeys.Weapon1;
                    SetActiveWeapon(index);
                }

                if (command.Key == GameControlKeys.PreviousWeapon)
                    SetActivePreviousWeapon();
                if (command.Key == GameControlKeys.NextWeapon)
                    SetActiveNextWeapon();
                if (command.Key == GameControlKeys.Fire1)
                    WeaponTryFire(false);
                if (command.Key == GameControlKeys.Fire2)
                    WeaponTryFire(true);
                if (command.Key == GameControlKeys.Reload)
                    WeaponTryReload();
            }
        }

        [Browsable(false)]
        public Weapon ActiveWeapon
        {
            get { return activeWeapon; }
        }

        void WeaponTryFire(bool alternative)
        {
            if (activeWeapon == null)
                return;

            //set real weapon fire direction
            {
                //!!!!!mb not true use DefaultCamera here
                Vec3 seeDir = SeePosition - RendererWorld.Instance.DefaultCamera.Position;

                Vec3 lookTo = SeePosition;

                for (int iter = 0; iter < 100; iter++)
                {
                    activeWeapon.SetForceFireRotationLookTo(lookTo);
                    Vec3 fireDir = activeWeapon.GetFireRotation(alternative) * new Vec3(1, 0, 0);
                    Degree angle = MathUtils.GetVectorsAngle(seeDir, fireDir);

                    if (angle < 80)
                        break;
                    const float step = .3f;
                    lookTo += seeDir * step;
                }

                activeWeapon.SetForceFireRotationLookTo(lookTo);
            }

            bool fired = activeWeapon.TryFire(alternative);

            Gun activeGun = activeWeapon as Gun;
            if (activeGun != null)
            {
                if (fired)
                {
                    int index = GetWeaponIndex(activeWeapon.Type);
                    weapons[index].normalBulletCount = activeGun.NormalMode.BulletCount;
                    weapons[index].normalMagazineCount = activeGun.NormalMode.BulletMagazineCount;
                    weapons[index].alternativeBulletCount = activeGun.AlternativeMode.BulletCount;
                    weapons[index].alternativeMagazineCount =
                        activeGun.AlternativeMode.BulletMagazineCount;
                }
            }
        }

        void WeaponTryReload()
        {
            if (activeWeapon == null)
                return;

            Gun activeGun = activeWeapon as Gun;
            if (activeGun != null)
                activeGun.TryReload();
        }

        public ChangeMapInformation GetChangeMapInformation(MapChangeRegion region)
        {
            ChangeMapInformation information = new ChangeMapInformation();

            information.position = (OldPosition - region.Position) * region.Rotation.GetInverse();
            information.lookDirection = ((PlayerIntellect)Intellect).LookDirection *
                region.Rotation.GetInverse();
            information.velocity = MainBody.LinearVelocity * region.Rotation.GetInverse();

            information.life = Life;

            information.weapons = weapons;
            information.activeWeaponIndex = GetWeaponIndex(
                (activeWeapon != null) ? activeWeapon.Type : null);

            return information;
        }

        public void ApplyChangeMapInformation(ChangeMapInformation information,
            SpawnPoint spawnPoint)
        {
            if (spawnPoint == null)
                return;

            Position = spawnPoint.Position + information.position * spawnPoint.Rotation;
            ((PlayerIntellect)Intellect).LookDirection =
                information.lookDirection * spawnPoint.Rotation;
            MainBody.LinearVelocity = information.velocity * spawnPoint.Rotation;

            OldPosition = Position;
            OldRotation = Rotation;

            weapons = information.weapons;
            SetActiveWeapon(information.activeWeaponIndex);

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

        protected override void OnCopyTransform(MapObject from)
        {
            base.OnCopyTransform(from);
            SetTurnToPosition(from.Position + from.Rotation * new Vec3(100, 0, 0));
        }

    }
}
