//// Copyright (C) 2006-2008 NeoAxis Group Ltd.
//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Diagnostics;
//using System.ComponentModel;
//using System.Drawing.Design;
//using Engine;
//using Engine.EntitySystem;
//using Engine.MapSystem;
//using Engine.PhysicsSystem;
//using Engine.MathEx;
//using Engine.Renderer;
//using Engine.Utils;

//namespace GameEntities
//{
//   /// <summary>
//   /// Defines the <see cref="FreeCameraCollider"/> entity type.
//   /// </summary>
//   public class FreeCameraColliderType : MapObjectType
//   {
//      const float radiusDefault = .4f;
//      [FieldSerialize]
//      float radius = radiusDefault;

//      const float linearDampingDefault = 7;
//      [FieldSerialize]
//      float linearDamping = linearDampingDefault;

//      const float massDefault = 1;
//      [FieldSerialize]
//      float mass = massDefault;

//      const float maxFlyVelocityDefault = 20;
//      [FieldSerialize]
//      float maxFlyVelocity = maxFlyVelocityDefault;

//      const float flyForceDefault = 5000;
//      [FieldSerialize]
//      float flyForce = flyForceDefault;

//      //

//      [DefaultValue( radiusDefault )]
//      public float Radius
//      {
//         get { return radius; }
//         set { radius = value; }
//      }

//      [DefaultValue( linearDampingDefault )]
//      public float LinearDamping
//      {
//         get { return linearDamping; }
//         set { linearDamping = value; }
//      }

//      [DefaultValue( massDefault )]
//      public float Mass
//      {
//         get { return mass; }
//         set { mass = value; }
//      }

//      [DefaultValue( maxFlyVelocityDefault )]
//      public float FlyMaxVelocity
//      {
//         get { return maxFlyVelocity; }
//         set { maxFlyVelocity = value; }
//      }

//      [DefaultValue( flyForceDefault )]
//      public float FlyForce
//      {
//         get { return flyForce; }
//         set { flyForce = value; }
//      }
//   }

//   public class FreeCameraCollider : MapObject
//   {
//      Body mainBody;

//      float moveForce;
//      SphereDir moveDirection;
//      SphereDir lookDirection;

//      bool noWakeBodies;

//      //

//      FreeCameraColliderType _type = null; public new FreeCameraColliderType Type { get { return _type; } }

//      [Browsable( false )]
//      public float MoveForce
//      {
//         get { return moveForce; }
//         set { moveForce = value; }
//      }

//      [Browsable( false )]
//      public SphereDir MoveDirection
//      {
//         get { return moveDirection; }
//         set { moveDirection = value; }
//      }

//      [Browsable( false )]
//      public SphereDir LookDirection
//      {
//         get { return lookDirection; }
//         set { lookDirection = value; }
//      }

//      [Browsable( false )]
//      public Body MainBody
//      {
//         get { return mainBody; }
//      }

//      /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
//      protected override void OnPostCreate( bool loaded )
//      {
//         base.OnPostCreate( loaded );

//         CreatePhysicsModel();

//         Body body = PhysicsModel.CreateBody();
//         mainBody = body;
//         body.Name = "main";
//         body.Position = Position;
//         body.Rotation = Rotation;
//         body.LinearDamping = Type.LinearDamping;

//         SphereShape shape = body.CreateSphereShape();
//         shape.Radius = Type.Radius;
//         shape.ContactGroup = (int)ContactGroup.Dynamic;
//         shape.StaticFriction = 0;
//         shape.DynamicFriction = 0;
//         shape.Bounciness = 0;
//         shape.Hardness = 0;
//         float r = shape.Radius;
//         shape.Density = Type.Mass / ( ( 4.0f / 3.0f ) * MathFunctions.PI * r * r * r );
//         shape.SpecialLiquidDensity = .5f;

//         PhysicsModel.PushToWorld();

//         AddTimer();
//      }

//      /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
//      protected override void OnTick()
//      {
//         base.OnTick();

//         mainBody.Sleeping = moveForce == 0 && mainBody.LinearVelocity.Length() < .3f;
//         mainBody.AngularVelocity = Vec3.Zero;

//         if( moveForce != 0 )
//         {
//            Vec3 forceVector = ( moveDirection.GetVector() * Rotation ) * moveForce;
//            float maxVelocity = Type.FlyMaxVelocity * forceVector.Length();

//            if( mainBody.LinearVelocity.Length() < maxVelocity )
//            {
//               mainBody.AddForce( ForceType.Global, 0,
//                  forceVector * Type.FlyForce * TickDelta, Vec3.Zero );
//            }
//         }

//         if( !mainBody.Sleeping )
//         {
//            //anti gravity
//            mainBody.AddForce( ForceType.GlobalAtLocalPos, TickDelta,
//               -PhysicsWorld.Instance.Gravity * mainBody.Mass, Vec3.Zero );
//         }

//         UpdateRotation();
//      }

//      void UpdateRotation()
//      {
//         Quat rot = Quat.FromDirectionZAxisUp( lookDirection.GetVector() );

//         noWakeBodies = true;
//         Rotation = rot;
//         noWakeBodies = false;
//         OldRotation = rot;
//      }

//      protected override void OnSetTransform( ref Vec3 pos, ref Quat rot, ref Vec3 scl )
//      {
//         base.OnSetTransform( ref pos, ref rot, ref scl );

//         if( PhysicsModel != null && !noWakeBodies )
//         {
//            foreach( Body body in PhysicsModel.Bodies )
//               body.Sleeping = false;
//         }
//      }
//   }
//}
