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
//   /// Defines the <see cref="SimpleCharacter"/> entity type.
//   /// </summary>
//   public class SimpleCharacterType : MapObjectType
//   {
//      const float heightDefault = 1.8f;
//      [FieldSerialize]
//      float height = heightDefault;

//      const float radiusDefault = .4f;
//      [FieldSerialize]
//      float radius = radiusDefault;

//      const float bottomRadiusDefault = .05f;
//      [FieldSerialize]
//      float bottomRadius = bottomRadiusDefault;

//      const float walkUpHeightDefault = .5f;
//      [FieldSerialize]
//      float walkUpHeight = walkUpHeightDefault;

//      const float massDefault = 1;
//      [FieldSerialize]
//      float mass = massDefault;

//      const float walkMaxVelocityDefault = 20;
//      [FieldSerialize]
//      float walkMaxVelocity = walkMaxVelocityDefault;

//      const float walkForceDefault = 1500;
//      [FieldSerialize]
//      float walkForce = walkForceDefault;

//      float floorHeight;//height from center to floor

//      //

//      [DefaultValue( heightDefault )]
//      public float Height
//      {
//         get { return height; }
//         set { height = value; }
//      }

//      [DefaultValue( radiusDefault )]
//      public float Radius
//      {
//         get { return radius; }
//         set { radius = value; }
//      }

//      [DefaultValue( bottomRadiusDefault )]
//      public float BottomRadius
//      {
//         get { return bottomRadius; }
//         set { bottomRadius = value; }
//      }

//      [DefaultValue( walkUpHeightDefault )]
//      public float WalkUpHeight
//      {
//         get { return walkUpHeight; }
//         set { walkUpHeight = value; }
//      }

//      [DefaultValue( massDefault )]
//      public float Mass
//      {
//         get { return mass; }
//         set { mass = value; }
//      }

//      [DefaultValue( walkMaxVelocityDefault )]
//      public float WalkMaxVelocity
//      {
//         get { return walkMaxVelocity; }
//         set { walkMaxVelocity = value; }
//      }

//      [DefaultValue( walkForceDefault )]
//      public float WalkForce
//      {
//         get { return walkForce; }
//         set { walkForce = value; }
//      }

//      [Browsable( false )]
//      public float FloorHeight
//      {
//         get { return floorHeight; }
//      }

//      protected override void OnLoaded()
//      {
//         base.OnLoaded();
//         floorHeight = ( height - walkUpHeight ) * .5f + walkUpHeight;
//      }
//   }

//   public class SimpleCharacter : MapObject
//   {
//      Body mainBody;

//      float moveForce;
//      SphereDir moveDirection;
//      SphereDir lookDirection;

//      float mainBodyGroundDistance = 1000;//from center of body
//      Body groundBody;

//      bool noWakeBodies;

//      //

//      SimpleCharacterType _type = null; public new SimpleCharacterType Type { get { return _type; } }

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

//      public bool IsOnGround()
//      {
//         return mainBodyGroundDistance - .05f < Type.FloorHeight && groundBody != null;
//      }

//      /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
//      protected override void OnPostCreate( bool loaded )
//      {
//         base.OnPostCreate( loaded );

//         lookDirection = new SphereDir( MathFunctions.DegToRad( -Rotation.ToAngles().Yaw ), 0 );

//         CreatePhysicsModel();

//         Body body = PhysicsModel.CreateBody();
//         mainBody = body;
//         body.Name = "main";
//         body.Position = Position;
//         body.Rotation = Rotation;
//         body.Sleepiness = 0;

//         float length = Type.Height - Type.Radius * 2 - Type.WalkUpHeight;
//         if( length < 0 )
//         {
//            Log.Error( "Length < 0" );
//            return;
//         }

//         //create main capsule
//         {
//            CapsuleShape shape = body.CreateCapsuleShape();
//            shape.Length = length;
//            shape.Radius = Type.Radius;
//            shape.ContactGroup = (int)ContactGroup.Dynamic;
//            shape.StaticFriction = 0;
//            shape.DynamicFriction = 0;
//            shape.Bounciness = 0;
//            shape.Hardness = 0;
//            float r = shape.Radius;
//            shape.Density = Type.Mass / ( MathFunctions.PI * r * r * shape.Length +
//               ( 4.0f / 3.0f ) * MathFunctions.PI * r * r * r );
//            shape.SpecialLiquidDensity = .5f;
//         }

//         //create down capsule
//         {
//            CapsuleShape shape = body.CreateCapsuleShape();
//            shape.Length = Type.Height - Type.BottomRadius * 2;
//            shape.Radius = Type.BottomRadius;
//            shape.Position = new Vec3( 0, 0,
//               ( Type.Height - Type.WalkUpHeight ) / 2 - Type.Height / 2 );
//            shape.ContactGroup = (int)ContactGroup.Dynamic;
//            shape.Bounciness = 0;
//            shape.Hardness = 0;
//            shape.Density = 0;
//            shape.SpecialLiquidDensity = .5f;

//            shape.StaticFriction = 0;
//            shape.DynamicFriction = 0;
//         }

//         //That the body did not fall after loading a map. 
//         //After loading a map, the physics simulate 5 seconds, that bodies have fallen asleep.
//         if( loaded )
//            body.Static = true;

//         PhysicsModel.PushToWorld();

//         AddTimer();
//      }

//      /// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnTick()"/>.</summary>
//      protected override void OnTick()
//      {
//         base.OnTick();

//         //That the body did not fall after loading a map. 
//         //After loading a map, the physics simulate 5 seconds, that bodies have fallen asleep.
//         if( PhysicsModel != null )
//            foreach( Body body in PhysicsModel.Bodies )
//               body.Static = false;

//         TickMovement();
//         TickControlForce();
//         UpdateRotation();
//      }

//      void TickControlForce()
//      {
//         if( moveForce != 0 && IsOnGround() )
//         {
//            Vec3 forceVector = ( moveDirection.GetVector() * Rotation ) * moveForce;
//            float maxVelocity = Type.WalkMaxVelocity * forceVector.Length();

//            if( mainBody.LinearVelocity.Length() < maxVelocity )
//            {
//               mainBody.AddForce( ForceType.Global, 0, new Vec3( forceVector.X, forceVector.Y, 0 ) *
//                  Type.WalkForce * TickDelta, Vec3.Zero );
//            }
//         }
//      }

//      void UpdateRotation()
//      {
//         Quat rot = new Angles( 0, 0, MathFunctions.RadToDeg( -lookDirection.Horizontal ) ).ToQuat();

//         noWakeBodies = true;
//         Rotation = rot;
//         noWakeBodies = false;
//         OldRotation = rot;
//      }

//      void TickMovement()
//      {
//         if( groundBody != null && groundBody.IsDisposed )
//            groundBody = null;

//         if( mainBody.Sleeping && groundBody != null && !groundBody.Sleeping &&
//            ( groundBody.LinearVelocity.LengthSqr() > .3f ||
//            groundBody.AngularVelocity.LengthSqr() > .3f ) )
//         {
//            mainBody.Sleeping = false;
//         }

//         if( mainBody.Sleeping && IsOnGround() )
//            return;

//         //update body damping
//         if( IsOnGround() )
//            mainBody.LinearDamping = 10;
//         else
//            mainBody.LinearDamping = .15f;

//         if( IsOnGround() )
//         {
//            mainBody.AngularVelocity = Vec3.Zero;

//            if( mainBodyGroundDistance + .05f < Type.FloorHeight )
//            {
//               noWakeBodies = true;
//               Position = Position + new Vec3( 0, 0, Type.FloorHeight - mainBodyGroundDistance );
//               noWakeBodies = false;
//            }
//         }

//         float oldMainBodyGroundDistance = mainBodyGroundDistance;

//         //calculate mainBodyGroundDistance, bodyGround
//         {
//            mainBodyGroundDistance = 1000;
//            groundBody = null;
//            Vec3 groundBodyNormal = Vec3.Zero;

//            for( int n = 0; n < 5; n++ )
//            {
//               Vec3 offset = Vec3.Zero;

//               float step = Type.BottomRadius;

//               switch( n )
//               {
//               case 0: offset = new Vec3( 0, 0, 0 ); break;
//               case 1: offset = new Vec3( -step, -step, step ); break;
//               case 2: offset = new Vec3( step, -step, step ); break;
//               case 3: offset = new Vec3( step, step, step ); break;
//               case 4: offset = new Vec3( -step, step, step ); break;
//               }

//               Vec3 pos = Position - new Vec3( 0, 0, Type.FloorHeight -
//                  Type.WalkUpHeight + .01f ) + offset;
//               RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
//                  new Ray( pos, new Vec3( 0, 0, -Type.Height * 1.5f ) ),
//                  (int)mainBody.Shapes[ 0 ].ContactGroup );

//               if( piercingResult.Length == 0 )
//                  continue;

//               foreach( RayCastResult result in piercingResult )
//               {
//                  if( result.Shape.Body == mainBody )
//                     continue;

//                  float dist = Position.Z - result.Position.Z;
//                  if( dist < mainBodyGroundDistance )
//                  {
//                     mainBodyGroundDistance = dist;
//                     groundBody = result.Shape.Body;
//                     groundBodyNormal = result.Normal;
//                  }
//               }

//               //on dynamic ground velocity
//               if( IsOnGround() && groundBody != null )
//               {
//                  if( !groundBody.Static && !groundBody.Sleeping )
//                  {
//                     Vec3 groundVel = groundBody.LinearVelocity;

//                     Vec3 vel = mainBody.LinearVelocity;

//                     if( groundVel.X > 0 && vel.X >= 0 && vel.X < groundVel.X )
//                        vel.X = groundVel.X;
//                     else if( groundVel.X < 0 && vel.X <= 0 && vel.X > groundVel.X )
//                        vel.X = groundVel.X;

//                     if( groundVel.Y > 0 && vel.Y >= 0 && vel.Y < groundVel.Y )
//                        vel.Y = groundVel.Y;
//                     else if( groundVel.Y < 0 && vel.Y <= 0 && vel.Y > groundVel.Y )
//                        vel.Y = groundVel.Y;

//                     if( groundVel.Z > 0 && vel.Z >= 0 && vel.Z < groundVel.Z )
//                        vel.Z = groundVel.Z;
//                     else if( groundVel.Z < 0 && vel.Z <= 0 && vel.Z > groundVel.Z )
//                        vel.Z = groundVel.Z;

//                     mainBody.LinearVelocity = vel;

//                     //stupid anti damping
//                     mainBody.LinearVelocity += groundVel * .05f;
//                  }

//                  //max slope check
//                  const float maxSlopeCoef = .7f;// MathFunctions.Sin( new Degree( 60.0f ).InRadians() );
//                  if( groundBodyNormal.Z < maxSlopeCoef )
//                  {
//                     Vec3 vector = new Vec3( groundBodyNormal.X, groundBodyNormal.Y, 0 );
//                     if( vector != Vec3.Zero )
//                     {
//                        vector.Normalize();
//                        vector *= mainBody.Mass * 10;
//                        mainBody.AddForce( ForceType.GlobalAtLocalPos, TickDelta, vector, Vec3.Zero );

//                        groundBody = null;
//                     }
//                  }
//               }
//            }
//         }

//         //sleep if on ground and zero velocity

//         bool needSleep = false;

//         if( IsOnGround() )
//         {
//            bool groundStopped = groundBody.Sleeping ||
//               ( groundBody.LinearVelocity.LengthSqr() < .3f &&
//               groundBody.AngularVelocity.LengthSqr() < .3f );

//            if( groundStopped && mainBody.LinearVelocity.LengthSqr() < 1.0f )
//               needSleep = true;
//         }

//         mainBody.Sleeping = needSleep;
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
