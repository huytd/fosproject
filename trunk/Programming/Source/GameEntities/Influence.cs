// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.ComponentModel;
using System.Drawing.Design;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.Renderer;

namespace GameEntities
{
	/// <summary>
	/// Defines the <see cref="Influence"/> entity type.
	/// </summary>
	public class InfluenceType : EntityType
	{
		[FieldSerialize]
		string defaultParticleName;

		[FieldSerialize]
		Substance allowSubstance;

		//

		[Editor( typeof( EditorParticleUITypeEditor ), typeof( UITypeEditor ) )]
		public string DefaultParticleName
		{
			get { return defaultParticleName; }
			set { defaultParticleName = value; }
		}

		[DefaultValue( Substance.None )]
		public Substance AllowSubstance
		{
			get { return allowSubstance; }
			set { allowSubstance = value; }
		}
	}

	/// <summary>
	/// Influences are effects on objects. For example, the ability to burn a monster, 
	/// is implemented through the use of influences.
	/// </summary>
	public class Influence : Entity
	{
		[FieldSerialize]
		float remainingTime;
		MapObjectAttachedParticle defaultAttachedParticle;

		//

		InfluenceType _type = null; public new InfluenceType Type { get { return _type; } }

		public float RemainingTime
		{
			get { return remainingTime; }
			set { remainingTime = value; }
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnPostCreate(Boolean)"/>.</summary>
		protected override void OnPostCreate2( bool loaded )
		{
			base.OnPostCreate2( loaded );

			//We has initialization into OnPostCreate2, because we need to intialize after
			//parent entity initialized (Dynamic.OnPostCreate). It's need for world serialization.

			AddTimer();

			Dynamic parent = (Dynamic)Parent;

			bool existsAttachedObjects = false;

			//show attached objects for this influence
			foreach( MapObjectAttachedObject attachedObject in parent.AttachedObjects )
			{
				if( attachedObject.Alias == Type.Name )
				{
					attachedObject.Visible = true;
					existsAttachedObjects = true;
				}
			}

			if( !existsAttachedObjects )
			{
				//create default particle system
				if( !string.IsNullOrEmpty( Type.DefaultParticleName ) )
				{
					defaultAttachedParticle = new MapObjectAttachedParticle();
					defaultAttachedParticle.ParticleName = Type.DefaultParticleName;
					parent.Attach( defaultAttachedParticle );
				}
			}
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity.OnDestroy()"/>.</summary>
		protected override void OnDestroy()
		{
			Dynamic parent = (Dynamic)Parent;

			//hide attached objects for this influence
			foreach( MapObjectAttachedObject attachedObject in parent.AttachedObjects )
			{
				if( attachedObject.Alias == Type.Name )
					attachedObject.Visible = false;
			}

			//destroy default particle system
			if( defaultAttachedParticle != null )
			{
				parent.Detach( defaultAttachedParticle );
				defaultAttachedParticle = null;
			}

			base.OnDestroy();
		}

		/// <summary>Overridden from <see cref="Engine.EntitySystem.Entity"/>.</summary>
		protected override void OnTick()
		{
			base.OnTick();

			remainingTime -= TickDelta;
			if( remainingTime <= 0 )
			{
				SetShouldDelete();
				return;
			}
		}
	}
}
