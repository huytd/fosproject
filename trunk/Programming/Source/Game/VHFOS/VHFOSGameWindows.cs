// Based on Copyrighted Code From NeoAxis Game Engine (C) 2006-2008 NeoAxis Group Ltd.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;
using Engine;
using Engine.Renderer;
using Engine.MathEx;
using Engine.SoundSystem;
using Engine.UISystem;
using Engine.EntitySystem;
using Engine.MapSystem;
using Engine.PhysicsSystem;
using Engine.FileSystem;
using Engine.Utils;
using System.Collections;
using GameCommon;
using GameEntities;

namespace Game
{
    /// <summary>
    /// Defines a game window for Viet Heroes - Fight or Surrender games.
    /// </summary>
    class VHFOSGameWindows : GameWindow
    {
        enum CameraType
        {           
            TPS,
            FPS,
            Free,
            Count,
        }
        [Config("Camera", "cameraType")]
        static CameraType cameraType;

        [Config("Camera", "tpsCameraDistance")]
        static float tpsCameraDistance = 4;

        [Config("Camera", "tpsCameraCenterOffset")]
        static float tpsCameraCenterOffset = 1.6f;

        [Config("Camera", "tpsVehicleCameraDistance")]
        static float tpsVehicleCameraDistance = 8.7f;

        [Config("Camera", "tpsVehicleCameraCenterOffset")]
        static float tpsVehicleCameraCenterOffset = 3.8f;

        //For management of pressing of the player on switches and management ingame GUI
        const float playerUseDistance = 3;
        const float playerUseDistanceTPS = 10;

        //Current ingame GUI which with which the player can cooperate
        MapObjectAttachedGui currentAttachedGuiObject;
        
        //Which player can switch the current switch
        GameEntities.Switch currentSwitch;

        //Minimap
        //bool minimapChangeCameraPosition;
        EControl minimapControl;

        //For an opportunity to change an active unit and for work with float switches
        bool switchUsing;

        //HUD screen
        EControl hudControl;

        //Data for an opportunity of the player to control other objects. (for Example: Turret control)
        Unit currentSeeUnitAllowPlayerControl;

        //For optimization of search of the nearest point on a map curve.
        //only for GetNearestPointToMapCurve()
        MapCurve observeCameraMapCurvePoints;
        List<Vec3> observeCameraMapCurvePointsList = new List<Vec3>();

        //The list of ObserveCameraArea's for faster work
        List<ObserveCameraArea> observeCameraAreas = new List<ObserveCameraArea>();

        //Sound delete
        ////Inventory Icon
        //List <EControl> inventorySlotButton = new List<EControl>(12);

        protected override void OnAttach()
        {
            base.OnAttach();

            //To load the HUD screen
            hudControl = ControlDeclarationManager.Instance.CreateControl("Gui\\ActionHUD.gui");

            //Attach the HUD screen to the this window
            Controls.Add(hudControl);

            hudControl.Controls["Inventory"].Visible = false;

            //CutSceneManager specific
            if (CutSceneManager.Instance != null)
            {
                CutSceneManager.Instance.CutSceneEnableChange += delegate(CutSceneManager manager)
                {
                    if (manager.CutSceneEnable)
                    {
                        //Cut scene activated. All keys and buttons need to reset.
                        EngineApp.Instance.KeysAndMouseButtonUpAll();
                        GameControlsManager.Instance.DoKeyUpAll();
                    }
                };
            }

            //fill observeCameraRegions list
            foreach (Entity entity in Map.Instance.Children)
            {
                ObserveCameraArea area = entity as ObserveCameraArea;
                if (area != null)
                    observeCameraAreas.Add(area);
            }

            FreeCameraEnabled = cameraType == CameraType.Free;

            //add game specific console command
            EngineConsole.Instance.AddCommand("movePlayerUnitToCamera",
                ConsoleCommand_MovePlayerUnitToCamera);

            //accept commands of the player
            GameControlsManager.Instance.GameControlsEvent += GameControlsManager_GameControlsEvent;

            //Accept command from inventory
            {
                foreach (EControl control in hudControl.Controls["Inventory"].Controls)
                {
                    if (control.Name != String.Empty)
                        try
                        {
                            (control as EButton).Click += new EButton.ClickDelegate(InventoryItem_Click);
                        }
                        catch
                        {
                        }
                }                
            }

            //Add scrool bar handle
            EScrollBar itemScroll = hudControl.Controls["Inventory/InventoryVScrollBar"] as EScrollBar;
            itemScroll.ValueChange += new EScrollBar.ValueChangeDelegate(ScrollBarValue_Change);

            //minimap
            minimapControl = hudControl.Controls["Minimap"];            
            
            minimapControl.RenderUI += new RenderUIDelegate(Minimap_RenderUI);
            hudControl.Controls["Arrow"].BackTexture = null;
            hudControl.Controls["Arrow"].RenderUI += new RenderUIDelegate(MiniMapArrow_RenderUI);

            //EngineApp.Instance.ScreenGuiRenderer.AddText("Render minimap",
            //       new Vec2(.5f, .9f), HorizontalAlign.Center, VerticalAlign.Center);

            //EngineConsole.Instance.Print("Warning: Minimap render ", new ColorValue(1, 0, 0));

            InitCameraViewFromTarget();

        }
             
        /// <summary>
        /// This method handle the click event on inventory items
        /// </summary>
        /// <param name="sender">The button in the inventory box</param>
        public void InventoryItem_Click(EButton sender)
        {
            Unit playerUnit = GetPlayerUnit();            

            //If there is an item is hole try to swap or change item position
            if (playerUnit.Inventory.CurrentHoldItem != string.Empty)
            {

                if (playerUnit.Inventory.CurrentHoldItem.StartsWith("A") && !sender.Name.ToString().StartsWith("A"))
                    (playerUnit as PlayerCharacter).RemoveAllWeapon();

                Item item = playerUnit.Inventory.SwapItem(playerUnit.Inventory.CurrentHoldItem, sender.Name);
                
                if (!playerUnit.Inventory.CurrentHoldItem.StartsWith("A") && sender.Name.ToString().StartsWith("A"))
                    try
                    {
                        (item as WeaponItem).OnSelect(playerUnit);
                    }
                    catch
                    {                        
                    }

                
            }
            else
            {                
                //Try to select item
                playerUnit.Inventory.holdItem(sender.Name);                
            }
        }

        /// <summary>
        /// This function handle scroll bar value change event to control iventory item list
        /// </summary>
        /// <param name="sender"></param>
        public void ScrollBarValue_Change(EScrollBar sender)
        { 
            
        }

        protected override void OnDetach()
        {
            //accept commands of the player
            GameControlsManager.Instance.GameControlsEvent -= GameControlsManager_GameControlsEvent;

            //minimap
            if (minimapControl.BackTexture != null)
            {
                minimapControl.BackTexture.Dispose();
                minimapControl.BackTexture = null;
            }


            base.OnDetach();
        }

        void RotateGuiControl(EControl sender, float alpha, Texture guiTexture, GuiRenderer renderer)
        {
            Rect controlRect = sender.GetScreenRectangle();
            Rect screenMapRect = sender.GetScreenRectangle();
            Vec2 size = controlRect.GetSize();           
            float angle = MathFunctions.DegToRad(alpha);
            float smallRadius = 0.03f;
            float bigRadius = 0.8f;
            float width = 0.15f;

            Vec2 U = new Vec2(+(float)Math.Cos(angle), +(float)Math.Sin(angle));
            Vec2 V = new Vec2(-(float)Math.Sin(angle), +(float)Math.Cos(angle));
            Vec2 P1 = bigRadius * U + V * width / 2;
            Vec2 P2 = bigRadius * U - V * width / 2;
            Vec2 P3 = smallRadius * U + V * width / 2;
            Vec2 P4 = smallRadius * U - V * width / 2;

            float X1 = Vec2.Dot(P1, Vec2.XAxis);
            float Y1 = Vec2.Dot(P1, Vec2.YAxis);
            float X2 = Vec2.Dot(P2, Vec2.XAxis);
            float Y2 = Vec2.Dot(P2, Vec2.YAxis);
            float X3 = Vec2.Dot(P3, Vec2.XAxis);
            float Y3 = Vec2.Dot(P3, Vec2.YAxis);
            float X4 = Vec2.Dot(P4, Vec2.XAxis);
            float Y4 = Vec2.Dot(P4, Vec2.YAxis);

            X1 = controlRect.Left + (X1 + 1) / 2 * size.X ;
            Y1 = controlRect.Top + (Y1 + 1) / 2 * size.Y ;
            X2 = controlRect.Left + (X2 + 1) / 2 * size.X;
            Y2 = controlRect.Top + (Y2 + 1) / 2 * size.Y ;
            X3 = controlRect.Left + (X3 + 1) / 2 * size.X;
            Y3 = controlRect.Top + (Y3 + 1) / 2 * size.Y ;
            X4 = controlRect.Left + (X4 + 1) / 2 * size.X;
            Y4 = controlRect.Top + (Y4 + 1) / 2 * size.Y ;

            List<GuiRenderer.TriangleVertex> vert = new List<GuiRenderer.TriangleVertex>(6);
            vert.Add(new GuiRenderer.TriangleVertex(new Vec2(X1, Y1), new ColorValue(1, 1, 1, 1), new Vec2(0, 0)));
            vert.Add(new GuiRenderer.TriangleVertex(new Vec2(X2, Y2), new ColorValue(1, 1, 1, 1), new Vec2(1, 0)));
            vert.Add(new GuiRenderer.TriangleVertex(new Vec2(X4, Y4), new ColorValue(1, 1, 1, 1), new Vec2(0, 1)));
            vert.Add(new GuiRenderer.TriangleVertex(new Vec2(X3, Y3), new ColorValue(1, 1, 1, 1), new Vec2(1, 1)));
            vert.Add(new GuiRenderer.TriangleVertex(new Vec2(X4, Y4), new ColorValue(1, 1, 1, 1), new Vec2(0, 1)));
            vert.Add(new GuiRenderer.TriangleVertex(new Vec2(X2, Y2), new ColorValue(1, 1, 1, 1), new Vec2(1, 0)));
            renderer.AddTriangles(vert, guiTexture, false); 
        }

        Texture cameraTexture;
        Camera rmCamera;
        void InitCameraViewFromTarget()
        {
            int textureSize = 256;

            //Texture cameraTexture
            cameraTexture = TextureManager.Instance.Create(
                TextureManager.Instance.GetUniqueName("playerreviewbox"), Texture.Type.Type2D,
                new Vec2i(textureSize, textureSize), 1, 0, PixelFormat.R8G8B8, Texture.Usage.RenderTarget);

            RenderTexture renderTexture = cameraTexture.GetBuffer().GetRenderTarget();

            rmCamera = SceneManager.Instance.CreateCamera();

            rmCamera.ProjectionType = ProjectionTypes.Perspective;
            rmCamera.PolygonMode = PolygonMode.Wireframe;

            renderTexture.AddViewport(rmCamera);
        }

        void GetCameraViewFromTarget()
        {
            if (hudControl.Controls["Inventory"].Visible == false) return;

            PlayerCharacter player = GetPlayerUnit() as PlayerCharacter;
                        
            if (player == null) return;

            else if (player != null)
            {
                rmCamera.FixedUp = (Vec3.ZAxis);

                rmCamera.Visible = true;

                rmCamera.Position = player.Position + (player.Position - player.OldPosition)*10;
                rmCamera.LookAt(player.Position);

                rmCamera.PolygonMode = PolygonMode.Wireframe;
                rmCamera.NearClipDistance = 1.0f;
                rmCamera.FarClipDistance = 10.3f;
                hudControl.Controls["Inventory/PlayerReviewBox"].BackTexture = cameraTexture;   
            }
        }


        void MiniMapArrow_RenderUI(EControl sender, GuiRenderer renderer)
        {
            //Thanks Thor22 from NeoAxis Forum
            //http://www.neoaxisgroup.com/phpBB2/viewtopic.php?t=1321&start=0
            Angles playerRot = GetPlayerUnit().Rotation.ToAngles();
            playerRot.Normalize360();
            float PlayerRotation = playerRot.Yaw; //If don't work try to add 90 or 180
            
            Texture arrowTexture = sender.BackTexture;

            //EControl arrowControl = minimapControl.Controls["Arrow"];
            //renderer.SetTransform(new Vec3(1, 1, 1), new Quat(0f, 0f, 0.001f, 1f), new Vec3(1f, 1f, 1f));
            RotateGuiControl(sender, PlayerRotation, arrowTexture, renderer);            
        }

        // TASK: Draw minimap
        void Minimap_RenderUI(EControl sender, GuiRenderer renderer)
        {
            Rect screenMapRect = sender.GetScreenRectangle();

            //Get Map Rectange
            Bounds initialBounds = Map.Instance.InitialCollisionBounds;
            Rect mapRect = new Rect(initialBounds.Minimum.ToVec2(), initialBounds.Maximum.ToVec2());


            Vec2 mapSizeInv = new Vec2(1, 1) / mapRect.Size;

            Unit playerUnit = GetPlayerUnit();

            Rect rect = new Rect(playerUnit.MapBounds.Minimum.ToVec2(),
                                 playerUnit.MapBounds.Maximum.ToVec2() );

            //Get position in the worldmap 2d image
            float worldMapX = (playerUnit.Position.X + 500) / 1000 * 512;
            float worldMapY = (playerUnit.Position.Y + 500) / 1000 * 512;

            //Get begin cliped rectange postion in the worldmap 2d image
            float x1 = worldMapX - 56;
            float y1 = worldMapY - 56;

            float x2 = x1 + 128;
            float y2 = x2 + 128;

            //Convert to % unit : 0.00...1.00
            float fx1 = x1 / 512.0f;
            float fy1 = (float)y1 / 512.0f;

            float fx2 = x2 / 512.0f;
            float fy2 = y2 / 512.0f;
           
            Rect coordRect = new Rect(fx1, fy1, fx2, fy2);
            minimapControl.BackTextureCoord = coordRect;



        //    //draw units
        //    Vec2 screenPixel = new Vec2(1, 1) / new Vec2(EngineApp.Instance.VideoMode.Size.ToVec2());
        //    {
        //        ////Loading texture to the engine
        //        //Texture texture = null;
        //        //string mapDirectory = Path.GetDirectoryName(mapName);
        //        //string textureName = mapDirectory + "\\Data\\Minimap";
        //        //string textureFileName = "Minimap";
        //        //bool finded = false;
        //        //string[] extensions = new string[] { "dds", "tga", "png", "jpg" };
        //        //foreach (string extension in extensions)
        //        //{
        //        //    textureFileName = textureName + "." + extension;
        //        //    if (VirtualFile.Exists(textureFileName))
        //        //    {
        //        //        finded = true;
        //        //        break;
        //        //    }
        //        //}
        //        //if (finded)
        //        //    texture = TextureManager.Instance.Load(textureFileName);
                                
        //        Unit playerUnit = GetPlayerUnit();

        //        Rect rect = new Rect(playerUnit.MapBounds.Minimum.ToVec2(),
        //                             playerUnit.MapBounds.Maximum.ToVec2()
        //                             );

        //        rect -= mapRect.Minimum;
        //        rect.Minimum *= mapSizeInv;
        //        rect.Maximum *= mapSizeInv;

        //        rect.Minimum = new Vec2(rect.Minimum.X, 1.0f - rect.Minimum.Y);
        //        rect.Maximum = new Vec2(rect.Maximum.X, 1.0f - rect.Maximum.Y);

        //        rect.Minimum *= screenMapRect.Size;
        //        rect.Maximum *= screenMapRect.Size;

        //        rect += screenMapRect.Minimum;

        //        //increase 1 pixel
        //        rect.Maximum += new Vec2(screenPixel.X, -screenPixel.Y);

        //        ColorValue color;

        //        if (playerUnit.Intellect == null || playerUnit.Intellect.Faction == null)
        //            color = new ColorValue(1, 1, 0);
        //        else
        //            color = new ColorValue(1, 0, 0);

        //        renderer.AddQuad(rect, color);


        //    }

        //    //foreach (Entity entity in Map.Instance.Children)
        //    //{
        //    //    GameCharacter unit = entity as GameCharacter;

        //    //    if (unit == null)
        //    //        continue;

        //    //    Rect rect = new Rect(unit.MapBounds.Minimum.ToVec2(), unit.MapBounds.Maximum.ToVec2());

        //    //    rect -= mapRect.Minimum;
            ////    rect.Minimum *= mapSizeInv;
            ////    rect.Maximum *= mapSizeInv;
            ////    rect.Minimum = new Vec2(rect.Minimum.X, 1.0f - rect.Minimum.Y);
            ////    rect.Maximum = new Vec2(rect.Maximum.X, 1.0f - rect.Maximum.Y);
            ////    rect.Minimum *= screenMapRect.Size;
            ////    rect.Maximum *= screenMapRect.Size;
            ////    rect += screenMapRect.Minimum;

            ////    //increase 1 pixel
            ////    rect.Maximum += new Vec2(screenPixel.X, -screenPixel.Y);

            ////    ColorValue color;

            ////    if (unit.Intellect == null || unit.Intellect.Faction == null)
            ////        color = new ColorValue(1, 1, 0);
            ////    else
            ////        color = new ColorValue(1, 0, 0);

            ////    renderer.AddQuad(rect, color);
            ////}

        //    //Draw camera borders
        //    {
        //        //    Camera camera = RendererWorld.Instance.DefaultCamera;

        //        //    if (camera.Position.Z > 0)
        //        //    {

        //        //        Plane groundPlane = new Plane(0, 0, 1, 0);

        //        //        Vec2[] points = new Vec2[4];

        //        //        for (int n = 0; n < 4; n++)
        //        //        {
        //        //            Vec2 p = Vec2.Zero;

        //        //            switch (n)
        //        //            {
        //        //                case 0: p = new Vec2(0, 0); break;
        //        //                case 1: p = new Vec2(1, 0); break;
        //        //                case 2: p = new Vec2(1, 1); break;
        //        //                case 3: p = new Vec2(0, 1); break;
        //        //            }

        //        //            Ray ray = camera.GetCameraToViewportRay(p);

        //        //            float scale;
        //        //            groundPlane.RayIntersection(ray, out scale);

        //        //            Vec3 pos = ray.GetPointOnRay(scale);
        //        //            if (ray.Direction.Z > 0)
        //        //                pos = ray.Origin + ray.Direction.GetNormalize() * 10000;

        //        //            Vec2 point = pos.ToVec2();

        //        //            point -= mapRect.Minimum;
        //        //            point *= mapSizeInv;
        //        //            point = new Vec2(point.X, 1.0f - point.Y);
        //        //            point *= screenMapRect.Size;
        //        //            point += screenMapRect.Minimum;

        //        //            points[n] = point;
        //        //        }

        //        //        for (int n = 0; n < 4; n++)
        //        //            renderer.AddLine(points[n], points[(n + 1) % 4], new ColorValue(1, 1, 1),
        //        //                screenMapRect);
        //        //    }
        //    }
        }
        
        //VHFOS: to pickup item with shift + right mouse
        bool ShiftKeyPressed = false;
        protected override bool OnKeyDown(KeyEvent e)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnKeyDown(e);

            //currentAttachedGuiObject
            if (currentAttachedGuiObject != null)
            {
                if (currentAttachedGuiObject.ControlManager.DoKeyDown(e))
                    return true;
            }

            //camera type change
            if (e.Key == EKeys.C)
            {
                cameraType = (CameraType)((int)cameraType + 1);
                if (cameraType == CameraType.Count)
                    cameraType = (CameraType)0;

                if (GetPlayerUnit() == null)
                    cameraType = CameraType.Free;

                FreeCameraEnabled = cameraType == CameraType.Free;

                return true;
            }


            //VHFOS: press tab to show inventory box
            if (e.Key == EKeys.Tab)
            {
                if (hudControl.Controls["Inventory"].Visible)
                {
                    hudControl.Controls["Inventory"].Visible = false;
                    
                    //Clear hold selection if inventory closed
                    //Thanks SodanKerjuu for this code
                    GetPlayerUnit().Inventory.CurrentHoldItem = string.Empty;
                    ScreenControlManager.Instance.DefaultCursor = @"Cursors\default.png"; 

                    EntitySystemWorld.Instance.Simulation = true;
                    EngineApp.Instance.MouseRelativeMode = true;
                }
                else
                {
                    hudControl.Controls["Inventory"].Visible = true;

                    EntitySystemWorld.Instance.Simulation = false;
                    EngineApp.Instance.MouseRelativeMode = false;
                }
            }

            //VHFOS
            if (e.Key == EKeys.Shift)
            {
                ShiftKeyPressed = true;
            }

            //GameControlsManager
            if (EntitySystemWorld.Instance.Simulation)
            {
                if (GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled())
                {
                    if (GameControlsManager.Instance.DoKeyDown(e))
                        return true;
                }
            }

            return base.OnKeyDown(e);
        }

        protected override bool OnKeyPress(KeyPressEvent e)
        {
            //currentAttachedGuiObject
            if (currentAttachedGuiObject != null)
            {
                currentAttachedGuiObject.ControlManager.DoKeyPress(e);
                return true;
            }

            return base.OnKeyPress(e);
        }

        protected override bool OnKeyUp(KeyEvent e)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnKeyUp(e);

            //currentAttachedGuiObject
            if (currentAttachedGuiObject != null)
                currentAttachedGuiObject.ControlManager.DoKeyUp(e);

            //VHFOS
            if (e.Key == EKeys.Shift)
            {
                ShiftKeyPressed = false;
            }

            //GameControlsManager
            GameControlsManager.Instance.DoKeyUp(e);
            
            return base.OnKeyUp(e);
        }

        protected override bool OnMouseDown(EMouseButtons button)
        {                
            //If you click out side the inventory box, and have selected an item, drop that item
            if (GetPlayerUnit().Inventory.CurrentHoldItem != String.Empty)
            {
                Vec2i windowSize = EngineApp.Instance.VideoMode.Size;

                float ix = hudControl.Controls["Inventory"].Position.Value.X * windowSize.X;
                float iy = hudControl.Controls["Inventory"].Position.Value.Y * windowSize.Y;

                float mx = MousePosition.X * windowSize.X;
                float my = MousePosition.Y * windowSize.Y;

                float iw = hudControl.Controls["Inventory"].Size.Value.X;
                float ih = hudControl.Controls["Inventory"].Size.Value.Y;


                if (!(ix < mx && mx < ix + iw && 
                      iy < my && my < iy + ih))
                {                    
                    //Show item
                    GetPlayerUnit().Inventory.dropItem(GetPlayerUnit(), hudControl);                                       

                    //Make selected icon to mouse
                    ScreenControlManager.Instance.DefaultCursor = @"Cursors\default.png";                    
                }
            }

            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnMouseDown(button);

            //currentAttachedGuiObject
            if (currentAttachedGuiObject != null)
            {
                currentAttachedGuiObject.ControlManager.DoMouseDown(button);
                return true;
            }

            //GameControlsManager
            if (EntitySystemWorld.Instance.Simulation)
            {
                if (GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled())
                {
                    if (GameControlsManager.Instance.DoMouseDown(button))
                        return true;
                }
            }

            return base.OnMouseDown(button);
        }

        protected override bool OnMouseUp(EMouseButtons button)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnMouseUp(button);

            //currentAttachedGuiObject
            if (currentAttachedGuiObject != null)
                currentAttachedGuiObject.ControlManager.DoMouseUp(button);

            //Check and do item pick up action
            if(button == EMouseButtons.Right && ShiftKeyPressed )
            {
                Ray lookRay = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
                new Vec2(.5f, .5f));

                Vec3 lookFrom = lookRay.Origin;
                Vec3 lookDir = Vec3.Normalize(lookRay.Direction);

                //VHFOS How far an item can be pick up
                float distance = 20.0f;

                Unit playerUnit = GetPlayerUnit();

                RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
                    new Ray(lookFrom, lookDir * distance), (int)ContactGroup.CastOnlyContact);

                foreach (RayCastResult result in piercingResult)
                {
                    MapObject obj = MapSystemWorld.GetMapObjectByBody(result.Shape.Body);
                    
                    if (obj != null)
                        if (obj.Type != null)
                            if (obj.Type.Name.ToString().EndsWith("Item"))
                            {
                                Item pickItem = obj as Item;
                                if (playerUnit.Inventory.AddItem(pickItem))
                                {
                                    pickItem.Take(playerUnit as Unit);
                                    pickItem.Visible = false;
                                    pickItem.Position = new Vec3(0.0f, 0.0f, 10000.0f);
                                }
                            }
                }
            }

            //GameControlsManager
            GameControlsManager.Instance.DoMouseUp(button);

            return base.OnMouseUp(button);
        }

        protected override bool OnMouseDoubleClick(EMouseButtons button)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnMouseDoubleClick(button);

            //currentAttachedGuiObject
            if (currentAttachedGuiObject != null)
            {
                currentAttachedGuiObject.ControlManager.DoMouseDoubleClick(button);
                return true;
            }

            return base.OnMouseDoubleClick(button);
        }

        protected override bool OnMouseMove()
        {
            bool ret = base.OnMouseMove();

            //If atop openly any window to not process
            if (Controls.Count != 1)
                return ret;

            //ignore mouse move events if DebugInformationWindow enabled without background mode
            if (DebugInformationWindow.Instance != null && !DebugInformationWindow.Instance.Background)
                return ret;

            //GameControlsManager
            if (EntitySystemWorld.Instance.Simulation)
            {
                if (GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled())
                {
                    Vec2 mouseOffset = MousePosition;
                                        
                    GameControlsManager.Instance.DoMouseMoveRelative(mouseOffset);
                }
            }

            return ret;
        }

        protected override bool OnMouseWheel(int delta)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnMouseWheel(delta);

            //currentAttachedGuiObject
            if (currentAttachedGuiObject != null)
            {
                currentAttachedGuiObject.ControlManager.DoMouseWheel(delta);
                return true;
            }

            return base.OnMouseWheel(delta);
        }

        protected override bool OnJoystickEvent(JoystickInputEvent e)
        {
            //If atop openly any window to not process
            if (Controls.Count != 1)
                return base.OnJoystickEvent(e);

            //GameControlsManager
            if (EntitySystemWorld.Instance.Simulation)
            {
                if (GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled())
                {
                    if (GameControlsManager.Instance.DoJoystickEvent(e))
                        return true;
                }
            }

            return base.OnJoystickEvent(e);
        }

        protected override void OnTick(float delta)
        {
            base.OnTick(delta);

            //NeedWorldDestroy
            if (GameWorld.Instance.NeedWorldDestroy)
            {
                EntitySystemWorld.Instance.Simulation = false;
                MapSystemWorld.MapDestroy();
                EntitySystemWorld.Instance.WorldDestroy();
                ScreenControlManager.Instance.Controls.Clear();
                ScreenControlManager.Instance.Controls.Add(new MainMenuWindow());
                return;
            }

            //If atop openly any window to not process
            if (Controls.Count != 1)
                return;

            if (ScreenControlManager.Instance != null &&
                ScreenControlManager.Instance.IsControlFocused())
                return;

            //update mouse relative mode
            {
                if (GetRealCameraType() == CameraType.Free && !FreeCameraMouseRotating)
                    EngineApp.Instance.MouseRelativeMode = false;

                if (EntitySystemWorld.Instance.Simulation && GetRealCameraType() != CameraType.Free)
                    EngineApp.Instance.MouseRelativeMode = true;

                if (DebugInformationWindow.Instance != null && !DebugInformationWindow.Instance.Background)
                    EngineApp.Instance.MouseRelativeMode = false;
            }

            if (GetRealCameraType() == CameraType.TPS && !IsCutSceneEnabled() &&
                !EngineConsole.Instance.Active)
            {
                Range distanceRange = new Range(2, 200);
                Range centerOffsetRange = new Range(0, 10);

                float cameraDistance;
                float cameraCenterOffset;

                if (IsPlayerUnitVehicle())
                {
                    cameraDistance = tpsVehicleCameraDistance;
                    cameraCenterOffset = tpsVehicleCameraCenterOffset;
                }
                else
                {
                    cameraDistance = tpsCameraDistance;
                    cameraCenterOffset = tpsCameraCenterOffset;
                }

                if (EngineApp.Instance.IsKeyPressed(EKeys.PageUp))
                {
                    cameraDistance -= delta * distanceRange.Size() / 20.0f;
                    if (cameraDistance < distanceRange[0])
                        cameraDistance = distanceRange[0];
                }

                if (EngineApp.Instance.IsKeyPressed(EKeys.PageDown))
                {
                    cameraDistance += delta * distanceRange.Size() / 20.0f;
                    if (cameraDistance > distanceRange[1])
                        cameraDistance = distanceRange[1];
                }

                if (EngineApp.Instance.IsKeyPressed(EKeys.Home))
                {
                    cameraCenterOffset += delta * centerOffsetRange.Size() / 4.0f;
                    if (cameraCenterOffset > centerOffsetRange[1])
                        cameraCenterOffset = centerOffsetRange[1];
                }

                if (EngineApp.Instance.IsKeyPressed(EKeys.End))
                {
                    cameraCenterOffset -= delta * centerOffsetRange.Size() / 4.0f;
                    if (cameraCenterOffset < centerOffsetRange[0])
                        cameraCenterOffset = centerOffsetRange[0];
                }

                if (IsPlayerUnitVehicle())
                {
                    tpsVehicleCameraDistance = cameraDistance;
                    tpsVehicleCameraCenterOffset = cameraCenterOffset;
                }
                else
                {
                    tpsCameraDistance = cameraDistance;
                    tpsCameraCenterOffset = cameraCenterOffset;
                }
            }

            //GameControlsManager
            if (EntitySystemWorld.Instance.Simulation)
            {
                if (GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled())
                    GameControlsManager.Instance.DoTick(delta);
            }
        }

        /// <summary>
        /// Updates objects on which the player can to operate.
        /// Such as which the player can supervise switches, ingameGUI or control units.
        /// </summary>
        void UpdateCurrentPlayerUseObjects()
        {
            Camera camera = RendererWorld.Instance.DefaultCamera;

            Unit playerUnit = GetPlayerUnit();

            float maxDistance = (GetRealCameraType() == CameraType.FPS) ?
                playerUseDistance : playerUseDistanceTPS;

            Ray ray = camera.GetCameraToViewportRay(new Vec2(.5f, .5f));
            ray.Direction = ray.Direction.GetNormalize() * maxDistance;

            //currentAttachedGuiObject
            {
                MapObjectAttachedGui attachedGuiObject = null;
                Vec2 screenPosition = Vec2.Zero;

                if (GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() &&
                    EntitySystemWorld.Instance.Simulation)
                {
                    Map.Instance.GetObjectsAttachedGuiObject(ray,
                        out attachedGuiObject, out screenPosition);
                }

                //ignore empty gui objects
                if (attachedGuiObject != null)
                {
                    In3dControlManager manager = attachedGuiObject.ControlManager;

                    if (manager.Controls.Count == 0 ||
                        (manager.Controls.Count == 1 && !manager.Controls[0].Enable))
                    {
                        attachedGuiObject = null;
                    }
                }

                if (attachedGuiObject != currentAttachedGuiObject)
                {
                    if (currentAttachedGuiObject != null)
                        currentAttachedGuiObject.ControlManager.LostManagerFocus();
                    currentAttachedGuiObject = attachedGuiObject;
                }

                if (currentAttachedGuiObject != null)
                    currentAttachedGuiObject.ControlManager.DoMouseMove(screenPosition);
            }

            //currentFloatSwitch
            {
                GameEntities.Switch overSwitch = null;

                Map.Instance.GetObjects(ray, delegate(MapObject obj, float scale)
                {
                    GameEntities.Switch s = obj as GameEntities.Switch;
                    if (s != null)
                    {
                        if (s.UseAttachedMesh != null)
                        {
                            Bounds bounds = ((MapObjectAttachedMesh)s.UseAttachedMesh).SceneNode.
                                GetWorldBounds();

                            if (bounds.RayIntersection(ray))
                            {
                                overSwitch = s;
                                return false;
                            }
                        }
                        else
                        {
                            overSwitch = s;
                            return false;
                        }
                    }

                    return true;
                });

                //draw border
                if (overSwitch != null)
                {
                    camera.DebugGeometry.Color = new ColorValue(1, 1, 1);
                    if (overSwitch.UseAttachedMesh != null)
                    {
                        camera.DebugGeometry.AddBounds(overSwitch.UseAttachedMesh.SceneNode.
                            GetWorldBounds());
                    }
                    else
                        camera.DebugGeometry.AddBounds(overSwitch.MapBounds);
                }

                if (overSwitch != currentSwitch)
                {
                    FloatSwitch floatSwitch = currentSwitch as FloatSwitch;
                    if (floatSwitch != null)
                        floatSwitch.UseEnd();

                    currentSwitch = overSwitch;
                }
            }

            //Use player control unit
            if (playerUnit != null)
            {
                currentSeeUnitAllowPlayerControl = null;

                if (PlayerIntellect.Instance != null &&
                    PlayerIntellect.Instance.MainNoActivedPlayerUnit == null &&
                    GetRealCameraType() != CameraType.Free)
                {
                    Ray unitFindRay = ray;

                    //special ray for TPS camera
                    if (GetRealCameraType() == CameraType.TPS)
                    {
                        unitFindRay = new Ray(playerUnit.Position,
                            playerUnit.Rotation * new Vec3(playerUseDistance, 0, 0));
                    }

                    Map.Instance.GetObjects(unitFindRay, delegate(MapObject obj, float scale)
                    {
                        Dynamic dynamic = obj as Dynamic;

                        if (dynamic == null)
                            return true;

                        Unit u = dynamic.GetParentUnit();
                        if (u == null)
                            return true;

                        if (u == GetPlayerUnit())
                            return true;

                        if (!u.Type.AllowPlayerControl)
                            return true;

                        if (u.Intellect != null)
                            return true;

                        if (!u.MapBounds.RayIntersection(unitFindRay))
                            return true;

                        currentSeeUnitAllowPlayerControl = u;

                        return false;
                    });
                }

                //draw border
                if (currentSeeUnitAllowPlayerControl != null)
                {
                    camera.DebugGeometry.Color = new ColorValue(1, 1, 1);
                    camera.DebugGeometry.AddBounds(currentSeeUnitAllowPlayerControl.MapBounds);
                }
            }

            //draw "Press Use" text
            if (currentSwitch != null || currentSeeUnitAllowPlayerControl != null)
            {
                ColorValue color;
                if ((Time % 2) < 1)
                    color = new ColorValue(1, 1, 0);
                else
                    color = new ColorValue(0, 1, 0);

                EngineApp.Instance.ScreenGuiRenderer.AddText("Press \"Use\"",
                    new Vec2(.5f, .9f), HorizontalAlign.Center, VerticalAlign.Center, color);
            }

        }

        protected override void OnRender()
        {
            base.OnRender();

            UpdateHUD();
            GetCameraViewFromTarget();
            UpdateCurrentPlayerUseObjects();
        }

        void UpdateHUDControlIcon(EControl control, string iconName)
        {
            if (!string.IsNullOrEmpty(iconName))
            {
                string fileName = string.Format("Gui\\HUD\\Icons\\{0}.png", iconName);

                bool needUpdate = false;

                if (control.BackTexture != null)
                {
                    string current = control.BackTexture.Name;
                    current = current.Replace('/', '\\');

                    if (string.Compare(fileName, current, true) != 0)
                        needUpdate = true;
                }
                else
                    needUpdate = true;

                if (needUpdate)
                {
                    if (VirtualFile.Exists(fileName))
                        control.BackTexture = TextureManager.Instance.Load(fileName, Texture.Type.Type2D, 0);
                    else
                        control.BackTexture = null;
                }
            }
            else
                control.BackTexture = null;
        }

        /// <summary>
        /// Updates HUD screen
        /// </summary>
        void UpdateHUD()
        {
            Unit playerUnit = GetPlayerUnit();

            hudControl.Visible = Map.Instance.DrawGui;

            //Game

            hudControl.Controls["Game"].Visible = GetRealCameraType() != CameraType.Free &&
                !IsCutSceneEnabled();

            //Player
            string playerTypeName = playerUnit != null ? playerUnit.Type.Name : "";

            //UpdateHUDControlIcon(hudControl.Controls["Game/PlayerIcon"], playerTypeName);
            //Update player name
            //hudControl.Controls["Game/Player"].Text = playerTypeName;

            //PlayerCharacter specific
            if (playerUnit != null)
                playerUnit.Inventory.OnRender(hudControl);

            //HealthBar
            {
                float coef = 0;
                if (playerUnit != null)
                    coef = playerUnit.Life / playerUnit.Type.LifeMax;

                EControl healthBar = hudControl.Controls["Game/HealthBar"];
                Vec2 originalSize = new Vec2(256, 32);
                Vec2 interval = new Vec2(117, 304);
                float sizeX = (117 - 82) + coef * (interval[1] - interval[0]);
                healthBar.Size = new ScaleValue(ScaleType.Texture, new Vec2(sizeX, originalSize.Y));
                healthBar.BackTextureCoord = new Rect(0, 0, sizeX / originalSize.X, 1);
            }

            //EnergyBar
            {
                float coef = .3f;

                EControl energyBar = hudControl.Controls["Game/EnergyBar"];
                Vec2 originalSize = new Vec2(256, 32);
                Vec2 interval = new Vec2(117, 304);
                float sizeX = (117 - 82) + coef * (interval[1] - interval[0]);
                energyBar.Size = new ScaleValue(ScaleType.Texture, new Vec2(sizeX, originalSize.Y));
                energyBar.BackTextureCoord = new Rect(0, 0, sizeX / originalSize.X, 1);
            }

            //Weapon
            {
                string weaponName = "";
                string magazineCountNormal = "";
                string bulletCountNormal = "";
                string bulletCountAlternative = "";

                Weapon weapon = null;

                //PlayerCharacter specific
                PlayerCharacter playerCharacter = playerUnit as PlayerCharacter;
                if (playerCharacter != null)
                    weapon = playerCharacter.ActiveWeapon;

                //Turret specific
                Turret turret = playerUnit as Turret;
                if (turret != null)
                    weapon = turret.MainGun;

                if (weapon != null)
                {
                    weaponName = weapon.Type.FullName;

                    Gun gun = weapon as Gun;
                    if (gun != null)
                    {
                        if (gun.Type.NormalMode.BulletType != null)
                        {
                            //magazineCountNormal
                            if (gun.Type.NormalMode.MagazineCapacity != 0)
                                magazineCountNormal = gun.NormalMode.BulletMagazineCount.ToString() + "/" +
                                    gun.Type.NormalMode.MagazineCapacity.ToString();
                            //bulletCountNormal
                            if (gun.Type.NormalMode.BulletExpense != 0)
                                bulletCountNormal = (gun.NormalMode.BulletCount - gun.NormalMode.BulletMagazineCount).ToString() + "/" +
                                    gun.Type.NormalMode.BulletCapacity.ToString();
                        }

                        if (gun.Type.AlternativeMode.BulletType != null)
                        {
                            //bulletCountAlternative
                            if (gun.Type.AlternativeMode.BulletExpense != 0)
                                bulletCountAlternative = gun.AlternativeMode.BulletCount.ToString() + "/" +
                                    gun.Type.AlternativeMode.BulletCapacity.ToString();
                        }
                    }
                }

                hudControl.Controls["Game/Weapon"].Text = weaponName;
                hudControl.Controls["Game/WeaponMagazineCountNormal"].Text = magazineCountNormal;
                hudControl.Controls["Game/WeaponBulletCountNormal"].Text = bulletCountNormal;
                hudControl.Controls["Game/WeaponBulletCountAlternative"].Text = bulletCountAlternative;

                UpdateHUDControlIcon(hudControl.Controls["Game/WeaponIcon"], weaponName);
            }

            //CutScene
            {
                hudControl.Controls["CutScene"].Visible = IsCutSceneEnabled();

                if (CutSceneManager.Instance != null)
                {
                    //CutSceneFade
                    float fadeCoef = 0;
                    if (CutSceneManager.Instance != null)
                        fadeCoef = CutSceneManager.Instance.GetFadeCoefficient();
                    hudControl.Controls["CutSceneFade"].BackColor = new ColorValue(0, 0, 0, fadeCoef);

                    //Message
                    {
                        string text;
                        ColorValue color;
                        CutSceneManager.Instance.GetMessage(out text, out color);
                        if (text == null)
                            text = "";

                        ETextBox textBox = (ETextBox)hudControl.Controls["CutScene/Message"];
                        textBox.Text = text;
                        textBox.TextColor = color;
                    }
                }
            }
        }

        /// <summary>
        /// Draw a target at center of screen
        /// </summary>
        /// <param name="renderer"></param>
        void DrawTarget(GuiRenderer renderer)
        {
            renderer.AddText("o", new Vec2(.5f, .5f), HorizontalAlign.Center, VerticalAlign.Center);

            Ray lookRay = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
                new Vec2(.5f, .5f));

            Body body = null;

            Vec3 lookFrom = lookRay.Origin;
            Vec3 lookDir = Vec3.Normalize(lookRay.Direction);
            float distance = 1000.0f;

            Unit playerUnit = GetPlayerUnit();

            RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
                new Ray(lookFrom, lookDir * distance), (int)ContactGroup.CastOnlyContact);

            foreach (RayCastResult result in piercingResult)
            {
                bool ignore = false;

                MapObject obj = MapSystemWorld.GetMapObjectByBody(result.Shape.Body);
                
                                

                Dynamic dynamic = obj as Dynamic;
                if (dynamic != null && playerUnit != null && dynamic.GetParentUnit() == GetPlayerUnit())
                    ignore = true;

                if (!ignore)
                {
                    body = result.Shape.Body;
                    break;
                }
            }

            //renderer.AddText("[  ]", new Vec2(.5f, .5f), HorizontalAlign.Center, VerticalAlign.Center);

            if (body != null && body == null)
            {
                MapObject obj = MapSystemWorld.GetMapObjectByBody(body);

                
                
                
                if (obj != null && !(obj is StaticMesh) && !(obj is GameGuiObject))
                {
                    renderer.AddText(obj.Type.Name, new Vec2(.5f, .525f),
                        HorizontalAlign.Center, VerticalAlign.Center);

                    Dynamic dynamic = obj as Dynamic;
                    if (dynamic != null)
                    {
                        if (dynamic.Type.LifeMax != 0)
                        {
                            renderer.AddText("XXXX", new Vec2(.5f - .04f, .55f), HorizontalAlign.Left,
                                VerticalAlign.Center, new ColorValue(.5f, .5f, .5f, .5f));

                            float lifecoef = dynamic.Life / dynamic.Type.LifeMax;

                            renderer.AddText("IIIIIIIIII", new Vec2(.5f - .04f, .55f), HorizontalAlign.Left,
                                VerticalAlign.Center, new ColorValue(.5f, .5f, .5f, .5f));

                            float count = lifecoef * 10;
                            String s = "";
                            for (int n = 0; n < count; n++)
                                s += "I";

                            renderer.AddText(s, new Vec2(.5f - .04f, .55f),
                                HorizontalAlign.Left, VerticalAlign.Center, new ColorValue(0, 1, 0, 1));
                        }

                        if (dynamic.PhysicsModel != null)
                        {
                            float mass = 0;
                            foreach (Body s in dynamic.PhysicsModel.Bodies)
                                mass += s.Mass;
                            string ss = string.Format("mass {0}", mass);
                            renderer.AddText(ss, new Vec2(.5f - .04f, .6f),
                                HorizontalAlign.Left, VerticalAlign.Center, new ColorValue(0, 1, 0, 1));
                        }
                    }
                }

            }

            
        }

       

        /// <summary>
        /// To draw some information of a player
        /// </summary>
        /// <param name="renderer"></param>
        void DrawPlayerInformation(GuiRenderer renderer)
        {
            if (GetRealCameraType() == CameraType.Free)
                return;

            if (IsCutSceneEnabled())
                return;

            //debug draw an influences.
            {
                float posy = .8f;

                foreach (Entity entity in GetPlayerUnit().Children)
                {
                    Influence influence = entity as Influence;
                    if (influence == null)
                        continue;

                    renderer.AddText(influence.Type.Name, new Vec2(.7f, posy),
                        HorizontalAlign.Left, VerticalAlign.Center);

                    int count = (int)((float)influence.RemainingTime * 2.5f);
                    if (count > 50)
                        count = 50;
                    string str = "";
                    for (int n = 0; n < count; n++)
                        str += "I";

                    renderer.AddText(str, new Vec2(.85f, posy),
                        HorizontalAlign.Left, VerticalAlign.Center);

                    posy -= .025f;
                }
            }
        }

        //void DrawPlayersStatistics( GuiRenderer renderer )
        //{
        //   if( GetRealCameraType() == CameraType.Free )
        //      return;

        //   if( IsCutSceneEnabled() )
        //      return;

        //   renderer.AddQuad( new Rect( .1f, .2f, .9f, .8f ), new ColorValue( 0, 0, 1, .5f ) );

        //   float posy = .25f;

        //   foreach( Entity entity in PlayerManager.Instance.Children )
        //   {
        //      Player player = entity as Player;
        //      if( player == null )
        //         continue;

        //      renderer.AddText( player.PlayerName, new Vec2( .2f, posy ),
        //         HorizontalAlign.Left, VerticalAlign.Center );

        //      renderer.AddText( player.Frags.ToString(), new Vec2( .5f, posy ),
        //         HorizontalAlign.Center, VerticalAlign.Center );

        //      renderer.AddText( player.Ping.ToString(), new Vec2( .8f, posy ),
        //         HorizontalAlign.Center, VerticalAlign.Center );

        //      posy += .025f;
        //   }
        //}

        protected override void OnRenderUI(GuiRenderer renderer)
        {
            base.OnRenderUI(renderer);

            //Draw some HUD information
            if (GetPlayerUnit() != null)
            {
                if (GetRealCameraType() != CameraType.Free && !IsCutSceneEnabled() &&
                    GetActiveObserveCameraArea() == null)
                {
                    DrawTarget(renderer);
                }

                DrawPlayerInformation(renderer);

                //if( EngineApp.Instance.IsKeyPressed( Keys.Tab ) && !EngineConsole.Instance.Active )
                //   DrawPlayersStatistics( renderer );


            }
        }

        CameraType GetRealCameraType()
        {
            //Replacement the camera type depending on a current unit.
            Unit playerUnit = GetPlayerUnit();
            if (playerUnit != null)
            {
                //Turret specific
                if (playerUnit as Turret != null)
                {
                    if (cameraType == CameraType.FPS)
                        return CameraType.TPS;
                }

                //Crane specific
                if (playerUnit as Crane != null)
                {
                    if (cameraType == CameraType.TPS)
                        return CameraType.FPS;
                }

                
            }

            return cameraType;
        }

        Unit GetPlayerUnit()
        {
            if (PlayerIntellect.Instance == null)
                return null;
            return PlayerIntellect.Instance.ControlledObject;
        }

        bool SwitchUseStart()
        {
            if (switchUsing)
                return false;

            if (currentSwitch == null)
                return false;

            FloatSwitch floatSwitch = currentSwitch as FloatSwitch;
            if (floatSwitch != null)
                floatSwitch.UseStart();

            GameEntities.BooleanSwitch booleanSwitch = currentSwitch as GameEntities.BooleanSwitch;
            if (booleanSwitch != null)
                booleanSwitch.Value = !booleanSwitch.Value;

            switchUsing = true;

            return true;
        }

        void SwitchUseEnd()
        {
            switchUsing = false;

            if (currentSwitch == null)
                return;

            FloatSwitch floatSwitch = currentSwitch as FloatSwitch;
            if (floatSwitch != null)
                floatSwitch.UseEnd();
        }

        bool CurrentUnitAllowPlayerControlUse()
        {
            if (PlayerIntellect.Instance != null)
            {
                //change player unit
                if (currentSeeUnitAllowPlayerControl != null)
                {
                    PlayerIntellect.Instance.ChangeMainControlledUnit(
                        currentSeeUnitAllowPlayerControl);
                    return true;
                }

                //restore player unit
                if (PlayerIntellect.Instance.MainNoActivedPlayerUnit != null)
                {
                    PlayerIntellect.Instance.RestoreMainControlledUnit();
                    return true;
                }
            }
            return false;
        }

        bool IsCutSceneEnabled()
        {
            return CutSceneManager.Instance != null && CutSceneManager.Instance.CutSceneEnable;
        }

        protected override void OnGetCameraTransform(out Vec3 position, out Vec3 forward,
            out Vec3 up, ref Degree cameraFov)
        {
            position = Vec3.Zero;
            forward = Vec3.XAxis;
            up = Vec3.ZAxis;

            Unit unit = GetPlayerUnit();
            if (unit == null)
                return;

            PlayerIntellect.Instance.FPSCamera = false;

            //To use data about orientation the camera if the cut scene is switched on
            if (IsCutSceneEnabled())
                if (CutSceneManager.Instance.GetCamera(out position, out forward, out up, out cameraFov))
                    return;

            //To receive orientation the camera if the player is in a observe camera area
            if (GetActiveObserveCameraAreaCameraOrientation(out position, out forward, out up, ref cameraFov))
                return;

            Vec3 cameraLookDir = PlayerIntellect.Instance.LookDirection.GetVector();

            switch (GetRealCameraType())
            {

                case CameraType.TPS:
                    {
                        float cameraDistance;
                        float cameraCenterOffset;

                        if (IsPlayerUnitVehicle())
                        {
                            cameraDistance = tpsVehicleCameraDistance;
                            cameraCenterOffset = tpsVehicleCameraCenterOffset;
                        }
                        else
                        {
                            cameraDistance = tpsCameraDistance;
                            cameraCenterOffset = tpsCameraCenterOffset;
                        }

                        //To calculate orientation of a TPS camera.
                        Vec3 lookAt = unit.GetInterpolatedPosition() + new Vec3(0, 0, cameraCenterOffset);
                        Vec3 cameraPos = lookAt - cameraLookDir * cameraDistance;

                        RayCastResult[] piercingResult = PhysicsWorld.Instance.RayCastPiercing(
                            new Ray(lookAt, cameraPos - lookAt), (int)ContactGroup.CastOnlyContact);
                        foreach (RayCastResult result in piercingResult)
                        {
                            bool ignore = false;

                            MapObject obj = MapSystemWorld.GetMapObjectByBody(result.Shape.Body);

                            if (obj == unit)
                                ignore = true;

                            if ((lookAt - result.Position).LengthSqr() < .001f)
                                ignore = true;

                            //cut ignore objects here
                            //..

                            if (!ignore)
                            {
                                cameraPos = result.Position;
                                break;
                            }
                        }

                        position = cameraPos;
                        forward = (lookAt - position).GetNormalize();
                    }
                    break;

                case CameraType.FPS:
                    {
                        //To calculate orientation of a FPS camera.

                        if (unit is Turret)
                        {
                            //Turret specific
                            Gun mainGun = ((Turret)unit).MainGun;
                            position = mainGun.GetInterpolatedPosition();
                            position += unit.Type.FPSCameraOffset * mainGun.GetInterpolatedRotation();
                        }
                        else
                        {
                            //Characters, etc
                            position = unit.GetInterpolatedPosition();
                            position += unit.Type.FPSCameraOffset * unit.GetInterpolatedRotation();
                        }
                        forward = PlayerIntellect.Instance.LookDirection.GetVector();
                    }
                    break;
            }

            //To update data in player intellect about type of the camera
            PlayerIntellect.Instance.FPSCamera = GetRealCameraType() == CameraType.FPS;

            float cameraOffset;
            if (IsPlayerUnitVehicle())
                cameraOffset = tpsVehicleCameraCenterOffset;
            else
                cameraOffset = tpsCameraCenterOffset;

            PlayerIntellect.Instance.TPSCameraCenterOffset = cameraOffset;

            //zoom for Turret
            if (EngineApp.Instance.IsMouseButtonPressed(EMouseButtons.Right))
            {
                if (GetPlayerUnit() as Turret != null)
                    if (GetRealCameraType() == CameraType.TPS)
                        cameraFov /= 3;
            }
        }

        /// <summary>
        /// Finds observe area in which there is a player.
        /// </summary>
        /// <returns><b>ObserveCameraArea</b>if the player is in area; otherwise <b>null</b>.</returns>
        ObserveCameraArea GetActiveObserveCameraArea()
        {
            Unit unit = GetPlayerUnit();
            if (unit == null)
                return null;

            foreach (ObserveCameraArea area in observeCameraAreas)
            {
                //check invalid area
                if (area.MapCamera == null && area.MapCurve == null)
                    continue;

                if (area.GetBox().IsContainsPoint(unit.Position))
                    return area;
            }
            return null;
        }

        /// <summary>
        /// Finds the nearest point to a map curve.
        /// </summary>
        /// <param name="destPos">The point to which is searched the nearest.</param>
        /// <param name="mapCurve">The map curve.</param>
        /// <returns>The nearest point to a map curve.</returns>
        Vec3 GetNearestPointToMapCurve(Vec3 destPos, MapCurve mapCurve)
        {
            //Calculate cached points
            if (observeCameraMapCurvePoints != mapCurve)
            {
                observeCameraMapCurvePoints = mapCurve;

                observeCameraMapCurvePointsList.Clear();

                float curveLength = 0;
                {
                    ReadOnlyCollection<MapCurvePoint> points = mapCurve.Points;
                    for (int n = 0; n < points.Count - 1; n++)
                        curveLength += (points[n].Position - points[n + 1].Position).LengthFast();
                }

                float step = 1.0f / curveLength / 100;
                for (float c = 0; c < 1; c += step)
                    observeCameraMapCurvePointsList.Add(mapCurve.CalculateCurvePointByCoefficient(c));
            }

            //calculate nearest point
            Vec3 nearestPoint = Vec3.Zero;
            float nearestDistanceSqr = float.MaxValue;

            foreach (Vec3 point in observeCameraMapCurvePointsList)
            {
                float distanceSqr = (point - destPos).LengthSqr();
                if (distanceSqr < nearestDistanceSqr)
                {
                    nearestPoint = point;
                    nearestDistanceSqr = distanceSqr;
                }
            }
            return nearestPoint;
        }

        /// <summary>
        /// Receives orientation of the camera in the observe area of in which there is a player.
        /// </summary>
        /// <param name="position">The camera position.</param>
        /// <param name="forward">The forward vector.</param>
        /// <param name="up">The up vector.</param>
        /// <param name="cameraFov">The camera FOV.</param>
        /// <returns><b>true</b>if the player is in any area; otherwise <b>false</b>.</returns>
        bool GetActiveObserveCameraAreaCameraOrientation(out Vec3 position, out Vec3 forward,
            out Vec3 up, ref Degree cameraFov)
        {
            position = Vec3.Zero;
            forward = Vec3.XAxis;
            up = Vec3.ZAxis;

            ObserveCameraArea area = GetActiveObserveCameraArea();
            if (area == null)
                return false;

            Unit unit = GetPlayerUnit();

            if (area.MapCurve != null)
            {
                Vec3 unitPos = unit.GetInterpolatedPosition();
                Vec3 nearestPoint = GetNearestPointToMapCurve(unitPos, area.MapCurve);

                position = nearestPoint;
                forward = (unit.GetInterpolatedPosition() - position).GetNormalize();
                up = Vec3.ZAxis;

                if (area.MapCamera != null && area.MapCamera.Fov != 0)
                    cameraFov = area.MapCamera.Fov;
            }

            if (area.MapCamera != null)
            {
                position = area.MapCamera.Position;
                forward = area.MapCamera.Rotation * new Vec3(1, 0, 0);
                up = area.MapCamera.Rotation * new Vec3(0, 0, 1);

                if (area.MapCamera.Fov != 0)
                    cameraFov = area.MapCamera.Fov;
            }

            return true;
        }

        bool IsPlayerUnitVehicle()
        {
            return false;
        }

        static void ConsoleCommand_MovePlayerUnitToCamera(string arguments)
        {
            if (Map.Instance == null)
                return;
            if (PlayerIntellect.Instance == null)
                return;

            Unit unit = PlayerIntellect.Instance.ControlledObject;
            if (unit == null)
                return;

            Ray lookRay = RendererWorld.Instance.DefaultCamera.GetCameraToViewportRay(
                new Vec2(.5f, .5f));

            RayCastResult result = PhysicsWorld.Instance.RayCast(
                lookRay, (int)ContactGroup.CastOnlyContact);

            if (result.Shape != null)
                unit.Position = result.Position + new Vec3(0, 0, unit.MapBounds.GetSize().Z);
        }

        void GameControlsManager_GameControlsEvent(GameControlsEventData e)
        {
            //GameControlsKeyDownEventData
            {
                GameControlsKeyDownEventData evt = e as GameControlsKeyDownEventData;
                if (evt != null)
                {
                    //"Use" control key
                    if (evt.ControlKey == GameControlKeys.Use)
                    {
                        //currentAttachedGuiObject
                        if (currentAttachedGuiObject != null)
                        {
                            currentAttachedGuiObject.ControlManager.DoMouseDown(EMouseButtons.Left);
                            return;
                        }

                        //key down for switch use
                        if (SwitchUseStart())
                            return;

                        if (CurrentUnitAllowPlayerControlUse())
                            return;
                    }

                    return;
                }
            }

            //GameControlsKeyUpEventData
            {
                GameControlsKeyUpEventData evt = e as GameControlsKeyUpEventData;
                if (evt != null)
                {
                    //"Use" control key
                    if (evt.ControlKey == GameControlKeys.Use)
                    {
                        //currentAttachedGuiObject
                        if (currentAttachedGuiObject != null)
                            currentAttachedGuiObject.ControlManager.DoMouseUp(EMouseButtons.Left);

                        //key up for switch use
                        SwitchUseEnd();
                    }

                    return;
                }
            }
        }

    }
}
