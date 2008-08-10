// Copyright (C) 2006-2008 NeoAxis Group Ltd.
using System;
using System.Collections.Generic;
using System.Text;
using Engine;
using Engine.UISystem;
using Engine.Renderer;
using Engine.MathEx;
using Engine.MapSystem;
using Engine.Utils;
using GameCommon;
using GameEntities;

namespace Game
{
	/// <summary>
	/// Defines a window of options.
	/// </summary>
	public class OptionsWindow : EControl
	{
		EControl window;
		EComboBox comboBoxResolution;
		EComboBox comboBoxBitsPerPixel;
		EComboBox comboBoxInputDevices;

		//

		protected override void OnAttach()
		{
			base.OnAttach();

			EComboBox comboBox;
			EScrollBar scrollBar;
			ECheckBox checkBox;

			window = ControlDeclarationManager.Instance.CreateControl( "Gui\\OptionsWindow.gui" );
			Controls.Add( window );

			( (EButton)window.Controls[ "Options" ].Controls[ "Quit" ] ).Click += delegate( EButton sender )
			{
				SetShouldDetach();
			};

			//pageVideo
			{
				EControl pageVideo = window.Controls[ "TabControl" ].Controls[ "Video" ];

				VideoMode currentMode = EngineApp.Instance.VideoMode;

				//screenResolutionComboBox
				comboBox = (EComboBox)pageVideo.Controls[ "ScreenResolution" ];
				comboBoxResolution = comboBox;

				foreach( VideoMode mode in DisplaySettings.VideoModes )
				{
					if( mode.Size.X < 640 )
						continue;

					if( mode.ColorDepth != 32 )
						continue;

					comboBox.Items.Add( mode.Size.X.ToString() + "x" + mode.Size.Y.ToString() );

					if( mode.Size == currentMode.Size )
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
				}

				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					ChangeVideoMode();
				};

				comboBox = (EComboBox)pageVideo.Controls[ "BitsPerPixel" ];
				comboBoxBitsPerPixel = comboBox;
				comboBox.Items.Add( "16" );
				comboBox.Items.Add( "32" );
				comboBox.SelectedIndex = ( currentMode.ColorDepth == 32 ) ? 1 : 0;

				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					ChangeVideoMode();
				};

				//gamma
				scrollBar = (EScrollBar)pageVideo.Controls[ "Gamma" ];
				scrollBar.Value = GameEngineApp._Gamma;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					float value = float.Parse( sender.Value.ToString( "F1" ) );
					GameEngineApp._Gamma = value;
					pageVideo.Controls[ "GammaValue" ].Text = value.ToString( "F1" );
				};
				pageVideo.Controls[ "GammaValue" ].Text = GameEngineApp._Gamma.ToString( "F1" );

				//MaterialScheme
				{
					comboBox = (EComboBox)pageVideo.Controls[ "MaterialScheme" ];
					foreach( MaterialSchemes materialScheme in
						Enum.GetValues( typeof( MaterialSchemes ) ) )
					{
						comboBox.Items.Add( materialScheme.ToString() );

						if( GameEngineApp.MaterialScheme == materialScheme )
							comboBox.SelectedIndex = comboBox.Items.Count - 1;
					}
					comboBox.SelectedIndexChange += delegate( EComboBox sender )
					{
						if( sender.SelectedIndex != -1 )
							GameEngineApp.MaterialScheme = (MaterialSchemes)sender.SelectedIndex;
					};
				}

				//ShadowTechnique
				comboBox = (EComboBox)pageVideo.Controls[ "ShadowTechnique" ];
				foreach( ShadowTechniques shadowTechnique in
					Enum.GetValues( typeof( ShadowTechniques ) ) )
				{
					if( shadowTechnique == ShadowTechniques.ShadowmapLow )
						comboBox.Items.Add( "ShadowmapLow (Need SM2)" );
					else if( shadowTechnique == ShadowTechniques.ShadowmapHigh )
						comboBox.Items.Add( "ShadowmapHigh (Need SM3)" );
					else
						comboBox.Items.Add( shadowTechnique );

					if( GameEngineApp.ShadowTechnique == shadowTechnique )
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
				}
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					if( sender.SelectedIndex != -1 )
						GameEngineApp.ShadowTechnique = (ShadowTechniques)sender.SelectedIndex;
					//GameEngineApp.ShadowTechnique = (ShadowTechniques)sender.SelectedItem;
					UpdateShadowControlsEnable();
				};
				UpdateShadowControlsEnable();

				//ShadowFarDistanceUseMapSettings
				{
					checkBox = (ECheckBox)pageVideo.Controls[ "ShadowFarDistanceUseMapSettings" ];
					checkBox.Checked = GameEngineApp.ShadowFarDistanceUseMapSettings;
					checkBox.CheckedChange += delegate( ECheckBox sender )
					{
						GameEngineApp.ShadowFarDistanceUseMapSettings = sender.Checked;
						if( sender.Checked && Map.Instance != null )
							GameEngineApp.ShadowFarDistance = Map.Instance.InitializedShadowFarDistance;

						UpdateShadowControlsEnable();

						if( sender.Checked )
						{
							( (EScrollBar)pageVideo.Controls[ "ShadowFarDistance" ] ).Value =
								GameEngineApp.ShadowFarDistance;
							pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
								( (int)GameEngineApp.ShadowFarDistance ).ToString();
						}
					};
				}

				//ShadowFarDistance
				scrollBar = (EScrollBar)pageVideo.Controls[ "ShadowFarDistance" ];
				scrollBar.Value = GameEngineApp.ShadowFarDistance;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					GameEngineApp.ShadowFarDistance = sender.Value;
					pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
						( (int)GameEngineApp.ShadowFarDistance ).ToString();
				};
				pageVideo.Controls[ "ShadowFarDistanceValue" ].Text =
					( (int)GameEngineApp.ShadowFarDistance ).ToString();

				//ShadowColor
				scrollBar = (EScrollBar)pageVideo.Controls[ "ShadowColor" ];
				scrollBar.Value = ( GameEngineApp.ShadowColor.Red + GameEngineApp.ShadowColor.Green +
					GameEngineApp.ShadowColor.Blue ) / 3;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					float color = sender.Value;
					GameEngineApp.ShadowColor = new ColorValue( color, color, color, color );
				};

				////ShadowCameraSetup
				//comboBox = (EComboBox)pageVideo.Controls[ "ShadowCameraSetup" ];
				//foreach( ShadowTextureCameraSetups shadowCameraSetup in
				//   Enum.GetValues( typeof( ShadowTextureCameraSetups ) ) )
				//{
				//   comboBox.Items.Add( shadowCameraSetup );
				//   if( GameEngineApp.ShadowCameraSetup == shadowCameraSetup )
				//      comboBox.SelectedIndex = comboBox.Items.Count - 1;
				//}
				//comboBox.SelectedIndexChange += delegate( EComboBox sender )
				//{
				//   GameEngineApp.ShadowCameraSetup = (ShadowTextureCameraSetups)sender.SelectedItem;
				//};

				//Shadow2DTextureSize
				comboBox = (EComboBox)pageVideo.Controls[ "Shadow2DTextureSize" ];
				comboBox.Items.Add( 256 );
				comboBox.Items.Add( 512 );
				comboBox.Items.Add( 1024 );
				comboBox.Items.Add( 2048 );
				if( GameEngineApp.Shadow2DTextureSize == 256 )
					comboBox.SelectedIndex = 0;
				if( GameEngineApp.Shadow2DTextureSize == 512 )
					comboBox.SelectedIndex = 1;
				else if( GameEngineApp.Shadow2DTextureSize == 1024 )
					comboBox.SelectedIndex = 2;
				else if( GameEngineApp.Shadow2DTextureSize == 2048 )
					comboBox.SelectedIndex = 3;
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.Shadow2DTextureSize = (int)sender.SelectedItem;
				};

				//Shadow2DTextureCount
				comboBox = (EComboBox)pageVideo.Controls[ "Shadow2DTextureCount" ];
				for( int n = 0; n < 3; n++ )
				{
					int count = n + 1;
					comboBox.Items.Add( count );
					if( count == GameEngineApp.Shadow2DTextureCount )
						comboBox.SelectedIndex = n;
				}
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.Shadow2DTextureCount = (int)sender.SelectedItem;
				};

				//ShadowCubicTextureSize
				comboBox = (EComboBox)pageVideo.Controls[ "ShadowCubicTextureSize" ];
				comboBox.Items.Add( 256 );
				comboBox.Items.Add( 512 );
				comboBox.Items.Add( 1024 );
				comboBox.Items.Add( 2048 );
				if( GameEngineApp.ShadowCubicTextureSize == 256 )
					comboBox.SelectedIndex = 0;
				if( GameEngineApp.ShadowCubicTextureSize == 512 )
					comboBox.SelectedIndex = 1;
				else if( GameEngineApp.ShadowCubicTextureSize == 1024 )
					comboBox.SelectedIndex = 2;
				else if( GameEngineApp.ShadowCubicTextureSize == 2048 )
					comboBox.SelectedIndex = 3;
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.ShadowCubicTextureSize = (int)sender.SelectedItem;
				};

				//ShadowCubicTextureCount
				comboBox = (EComboBox)pageVideo.Controls[ "ShadowCubicTextureCount" ];
				for( int n = 0; n < 3; n++ )
				{
					int count = n + 1;
					comboBox.Items.Add( count );
					if( count == GameEngineApp.ShadowCubicTextureCount )
						comboBox.SelectedIndex = n;
				}
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.ShadowCubicTextureCount = (int)sender.SelectedItem;
				};

				//fullScreenCheckBox
				checkBox = (ECheckBox)pageVideo.Controls[ "FullScreen" ];
				checkBox.Checked = EngineApp.Instance.FullScreen;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					EngineApp.Instance.FullScreen = sender.Checked;
				};

				//waterReflectionLevel
				comboBox = (EComboBox)pageVideo.Controls[ "WaterReflectionLevel" ];
				foreach( WaterPlane.ReflectionLevels level in Enum.GetValues(
					typeof( WaterPlane.ReflectionLevels ) ) )
				{
					comboBox.Items.Add( level );
					if( GameEngineApp.WaterReflectionLevel == level )
						comboBox.SelectedIndex = comboBox.Items.Count - 1;
				}
				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					GameEngineApp.WaterReflectionLevel = (WaterPlane.ReflectionLevels)sender.SelectedItem;
				};

				//showSystemCursorCheckBox
				checkBox = (ECheckBox)pageVideo.Controls[ "ShowSystemCursor" ];
				checkBox.Checked = GameEngineApp._ShowSystemCursor;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					GameEngineApp._ShowSystemCursor = sender.Checked;
					sender.Checked = GameEngineApp._ShowSystemCursor;
				};

				//showFPSCheckBox
				checkBox = (ECheckBox)pageVideo.Controls[ "ShowFPS" ];
				checkBox.Checked = GameEngineApp._DrawFPS;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					GameEngineApp._DrawFPS = sender.Checked;
					sender.Checked = GameEngineApp._DrawFPS;
				};

			}

			//pageSound
			{
				EControl pageSound = window.Controls[ "TabControl" ].Controls[ "Sound" ];

				//soundVolumeCheckBox
				scrollBar = (EScrollBar)pageSound.Controls[ "SoundVolume" ];
				scrollBar.Value = GameEngineApp.SoundVolume;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					GameEngineApp.SoundVolume = sender.Value;
				};

				//musicVolumeCheckBox
				scrollBar = (EScrollBar)pageSound.Controls[ "MusicVolume" ];
				scrollBar.Value = GameEngineApp.MusicVolume;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					GameEngineApp.MusicVolume = sender.Value;
				};
			}

			//pageControls
			{
				EControl pageControls = window.Controls[ "TabControl" ].Controls[ "Controls" ];

				//MouseHSensitivity
				scrollBar = (EScrollBar)pageControls.Controls[ "MouseHSensitivity" ];
				scrollBar.Value = GameControlsManager.Instance.MouseSensitivity.X;
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					value.X = sender.Value;
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//MouseVSensitivity
				scrollBar = (EScrollBar)pageControls.Controls[ "MouseVSensitivity" ];
				scrollBar.Value = Math.Abs( GameControlsManager.Instance.MouseSensitivity.Y );
				scrollBar.ValueChange += delegate( EScrollBar sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					bool invert = ( (ECheckBox)pageControls.Controls[ "MouseVInvert" ] ).Checked;
					value.Y = sender.Value * ( invert ? -1.0f : 1.0f );
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//MouseVInvert
				checkBox = (ECheckBox)pageControls.Controls[ "MouseVInvert" ];
				checkBox.Checked = GameControlsManager.Instance.MouseSensitivity.Y < 0;
				checkBox.CheckedChange += delegate( ECheckBox sender )
				{
					Vec2 value = GameControlsManager.Instance.MouseSensitivity;
					value.Y =
						( (EScrollBar)pageControls.Controls[ "MouseVSensitivity" ] ).Value *
						( sender.Checked ? -1.0f : 1.0f );
					GameControlsManager.Instance.MouseSensitivity = value;
				};

				//Devices
				comboBox = (EComboBox)pageControls.Controls[ "InputDevices" ];
				comboBoxInputDevices = comboBox;
				comboBox.Items.Add( "Keyboard/Mouse" );
				foreach( InputDevice device in InputDeviceManager.Instance.Devices )
					comboBox.Items.Add( device );
				comboBox.SelectedIndex = 0;

				comboBox.SelectedIndexChange += delegate( EComboBox sender )
				{
					UpdateBindedInputControlsTextBox();
				};

				//Controls
				UpdateBindedInputControlsTextBox();
			}

		}

		void UpdateShadowControlsEnable()
		{
			EControl pageVideo = window.Controls[ "TabControl" ].Controls[ "Video" ];

			bool textureShadows =
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapLow ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHigh ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapFFPModulative ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapFFPAdditive;
			bool allowShadowColor =
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapHigh ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapFFPModulative ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.ShadowmapFFPAdditive ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.StencilModulative ||
				GameEngineApp.ShadowTechnique == ShadowTechniques.StencilAdditive;

			pageVideo.Controls[ "ShadowColor" ].Enable = allowShadowColor;
			pageVideo.Controls[ "ShadowFarDistance" ].Enable =
				!GameEngineApp.ShadowFarDistanceUseMapSettings &&
				GameEngineApp.ShadowTechnique != ShadowTechniques.None;
			//pageVideo.Controls[ "ShadowCameraSetup" ].Enable = textureShadows;
			pageVideo.Controls[ "Shadow2DTextureSize" ].Enable = textureShadows;
			pageVideo.Controls[ "Shadow2DTextureCount" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowCubicTextureSize" ].Enable = textureShadows;
			pageVideo.Controls[ "ShadowCubicTextureCount" ].Enable = textureShadows;
		}

		void ChangeVideoMode()
		{
			Vec2i size;
			int bpp;
			{
				size = EngineApp.Instance.VideoMode.Size;

				if( comboBoxResolution.SelectedIndex != -1 )
				{
					string s = (string)( comboBoxResolution ).SelectedItem;
					s = s.Replace( "x", " " );
					size = Vec2i.Parse( s );
				}

				bpp = int.Parse( (string)( comboBoxBitsPerPixel ).SelectedItem );
			}

			EngineApp.Instance.VideoMode = new VideoMode( size, bpp );
		}

		void UpdateBindedInputControlsTextBox()
		{
			EControl pageControls = window.Controls[ "TabControl" ].Controls[ "Controls" ];

			//!!!!temp

			string text = "Configuring controls is not implemented\n";
			text += "\n";

			InputDevice inputDevice = comboBoxInputDevices.SelectedItem as InputDevice;

			text += "Default keys:\n";

			foreach( GameControlsManager.GameControlItem item in
				GameControlsManager.Instance.Items )
			{
				text += item.ControlKey.ToString();
				text += " - ";

				//keys and mouse buttons
				if( inputDevice == null )
				{
					foreach( GameControlsManager.SystemKeyboardMouseValue value in
						item.DefaultKeyboardMouseValues )
					{
						switch( value.Type )
						{
						case GameControlsManager.SystemKeyboardMouseValue.Types.Key:
							text += string.Format( "Key: {0},  ", value.Key );
							break;

						case GameControlsManager.SystemKeyboardMouseValue.Types.MouseButton:
							text += string.Format( "MouseButton: {0},  ", value.MouseButton );
							break;
						}
					}
				}

				//joystick
				JoystickInputDevice joystickInputDevice = inputDevice as JoystickInputDevice;
				if( joystickInputDevice != null )
				{
					foreach( GameControlsManager.SystemJoystickValue value in
						item.DefaultJoystickValues )
					{
						switch( value.Type )
						{
						case GameControlsManager.SystemJoystickValue.Types.Button:
							if( joystickInputDevice.GetButtonByName( value.Button ) != null )
								text += string.Format( "Button: {0},  ", value.Button );
							break;

						case GameControlsManager.SystemJoystickValue.Types.Axis:
							if( joystickInputDevice.GetAxisByName( value.Axis ) != null )
							{
								text += string.Format( "Axis: {0}({1}),  ",
									value.Axis, value.AxisFilter );
							}
							break;

						case GameControlsManager.SystemJoystickValue.Types.POV:
							if( joystickInputDevice.GetPOVByName( value.POV ) != null )
							{
								text += string.Format( "POV: {0}({1}),  ",
									value.POV, value.POVDirection );
							}
							break;
						}
					}
				}

				text += "\n";
			}

			pageControls.Controls[ "Controls" ].Text = text;
		}

		protected override bool OnKeyDown( KeyEvent e )
		{
			if( base.OnKeyDown( e ) )
				return true;
			if( e.Key == EKeys.Escape )
			{
				SetShouldDetach();
				return true;
			}
			return false;
		}

	}
}
