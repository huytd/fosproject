//Подключение необходимых библиотек
 using System.Collections.Generic;
 using Engine;
 using Engine.Renderer;
 using Engine.MathEx;
 using Engine.UISystem;
 using Engine.EntitySystem;
 using Engine.FileSystem;

namespace GameEntities
 {

  //Класс инвентаря
     public class EInventory : EControl
     {
         ///////////////////////////////////////////
 
   //Класс слота инвентаря
         public class ESlot
         {
    //Ссылка на кнопку слота
             EButton button;
             public EButton Button
             {
                 get { return button; }
                 set { button = value; }
             }
    //Ссылка на иконку слота
             EControl icon;
             public EControl Icon
             {
                 get { return icon; }
                 set { icon = value; }
             }
    //Индекс предмета
             int index = -1;
             public int Index
             {
                 get { return index; }
                 set { index = value; }
             }
    //Тип предмета
             ItemSlot slot = ItemSlot.None;
             public ItemSlot Slot
             {
                 get { return slot; }
                 set { slot = value; }
             }
    //True - если слот свободен, иначе - false
             public bool IsFree
             {
                 get { return index == -1; }
             }
    //Конструктор поумолчанию
             public ESlot()
             {
             }
    //Конструктор с параметром
             public ESlot( EButton Button )
             {
                 button = Button;
                 if( button != null )
                 {
                     icon = button.Controls[ "Icon" ];
                 }
             }
    //Конструктор с параметрами
             public ESlot( EButton Button, EControl Icon )
             {
                 button = Button;
                 icon = Icon;
             }
    //Функция копирует данные из объекта Source в текущий
             public void Copy( ESlot Source )
             {
                 index = Source.Index;
                 slot = Source.Slot;
                 icon.BackTexture = Source.Icon.BackTexture;
                 button.Text = Source.Button.Text;
             }
    //Функция меняет местами данные объектов Slot1 и Slot2
             public static void Swap( ESlot Slot1, ESlot Slot2 )
             {
                 ESlot Temp = new ESlot( new EButton(), new EControl() );
                 Temp.Copy( Slot1 );
                 Slot1.Copy( Slot2 );
                 Slot2.Copy( Temp );
             }
         }
 
         ///////////////////////////////////////////
 
   //Список слотов
         List<ESlot> Slots = new List<ESlot>();
   //Ссылка на персонажа игрока
         PlayerCharacter PlayerChar;
   //Ссылка на инвентарь
         EControl Inventory;
   //Ссылка на окно для вывода информации об объекте
         EEditBox Info;
   //Ссылка на слот с основным оружием
         ESlot MainWeapon;
   //Ссылка на слот с пистолетом
         ESlot Pistol;
   //Ссылка на слот для выбрасывания предмета
         ESlot Drop;
   //Ссылка на слот для уничтожения предмета
         ESlot Kill;
   //Ссылка на слот над которым сейчас курсор
         ESlot MouseOverSlot;
   //Ссылка на перетаскиваемый слот
         ESlot DragItem;
   //Время первого щелчка по слоту
         float FirstClickTime;
   //Время ожидания второго щелчка по слоту
         float Delay = 0.3f;
   //Функция вызываемая при открытии инвентаря
         protected override void OnAttach()
         {
             base.OnAttach();
    //Создание окна инвентаря и получение ссылки на него
             Inventory = ControlDeclarationManager.Instance.CreateControl( "GUI\\Inventory\\Inventory.gui" );
             Inventory.MouseCover = true;
             Controls.Add( Inventory );
    //Получение ссылки на персонажа игрока
             PlayerChar = GetPlayerUnit() as PlayerCharacter;
    //Получение ссылок на слоты основного оружия и пистолета
             MainWeapon = new ESlot( (EButton)Inventory.Controls[ "MainWeapon" ] );
             Pistol = new ESlot( (EButton)Inventory.Controls[ "Pistol" ] );
    //Получение ссылок на слоты выброса и уничтожения предмета
             Drop = new ESlot( (EButton)Inventory.Controls[ "Drop" ] );
             Kill = new ESlot( (EButton)Inventory.Controls[ "Kill" ] );
    //Загрузка иконок слотов выброса и уничтожения
             Drop.Icon.BackTexture = TextureManager.Instance.Load( "Gui\\Inventory\\Drop.png", Texture.Type.Type2D, 0 );
             Kill.Icon.BackTexture = TextureManager.Instance.Load( "Gui\\Inventory\\Kill.png", Texture.Type.Type2D, 0 );
    //Получение ссылки на окно информации
             Info = (EEditBox)Inventory.Controls[ "Info" ];
    //Заполнение списка слотов
             for( int i = 0; i < PlayerChar.InventorySize; i++ )
             {
                 Slots.Add( new ESlot( (EButton)Inventory.Controls[ "Slot" + i ] ) );
             }
    //Просмотр списка-инвентаря персонажа
             if(PlayerChar.Items != null)
             for( int i = 0; i < PlayerChar.Items.Count; i++ )
             {
     //Если слот не пустой
                 if( PlayerChar.Items[ i ].Index != -1 )
                 {
      //Получение ссылки на данный слот
                     ESlot Slot = GetSlot( "Slot" + i );
                     if( Slot != null )
                     {
                         string FileName = PlayerChar.Type.Items[ PlayerChar.Items[ i ].Index ].Icon;
       //Если есть иконка для данного предмета
                         if( FileExists( FileName ) )
                         {
        //Заполнение данных слота
                             Slot.Icon.BackTexture = TextureManager.Instance.Load( FileName, Texture.Type.Type2D, 0 );
                             Slot.Index = PlayerChar.Items[ i ].Index;
                             Slot.Slot = PlayerChar.Type.Items[ Slot.Index ].Slot;
                             if( PlayerChar.Items[ i ].Count > 1 )
                             {
                                 Slot.Button.Text = PlayerChar.Items[ i ].Count.ToString();
                             }
                         }
                     }
                 }
             }
    //Если слот с основным оружием не пустой
             if( PlayerChar.MainWeapon.Index != -1 )
             {
                 string FileName = PlayerChar.Type.Items[ PlayerChar.MainWeapon.Index ].Icon;
     //Если есть иконка данного предмета
                 if( FileExists( FileName ) )
                 {
      //Заполнение данных слота
                     MainWeapon.Icon.BackTexture = TextureManager.Instance.Load( FileName, Texture.Type.Type2D, 0 );
                     MainWeapon.Index = PlayerChar.MainWeapon.Index;
                     MainWeapon.Slot = PlayerChar.Type.Items[ MainWeapon.Index ].Slot;
                 }
             }
    //Если слот с пистолетом не пустой
             if( PlayerChar.Pistol.Index != -1 )
             {
                 string FileName = PlayerChar.Type.Items[ PlayerChar.Pistol.Index ].Icon;
     //Если есть иконка данного предмета
                 if( FileExists( FileName ) )
                 {
      //Заполнение данных слота
                     Pistol.Icon.BackTexture = TextureManager.Instance.Load( FileName, Texture.Type.Type2D, 0 );
                     Pistol.Index = PlayerChar.Pistol.Index;
                     Pistol.Slot = PlayerChar.Type.Items[ Pistol.Index ].Slot;
                 }
             }
    //Создание функции-обработчика нажатия на слоты
             for( int i = 0; i < PlayerChar.InventorySize; i++ )
             {
                 Slots[ i ].Button.Click += new EButton.ClickDelegate( Slot_Click );
             }
             MainWeapon.Button.Click += new EButton.ClickDelegate( Slot_Click );
             Pistol.Button.Click += new EButton.ClickDelegate( Slot_Click );
             Drop.Button.Click += new EButton.ClickDelegate( Slot_Click );
             Kill.Button.Click += new EButton.ClickDelegate( Slot_Click );
    //Функция обработчик нажатия на кнопку "Закрыть"
             ( (EButton)Inventory.Controls[ "Close" ] ).Click += delegate( EButton sender )
             {
     //Если инвентарь открыт
                 if( PlayerChar.Inventory != null )
                 {
      //Удаление окна и продолжение игрового процесса
                     PlayerChar.Inventory.SetShouldDetach();
                     PlayerChar.Inventory = null;
 
                     EntitySystemWorld.Instance.Simulation = true;
                     EngineApp.Instance.MouseRelativeMode = true;
                 }
             };
         }
   //Функция периодично вызываемая (период зависит от FPS)
         protected override void OnTick( float Delta )
         {
             base.OnTick( Delta );
    //Если курсор находится над слотом
             if( MouseOverSlot != null )
             {
     //В информационном окне выводится соответствующая информация
                 Info.Text = "";
                 if( MouseOverSlot == Kill )
                 {
                     Info.Text += "Уничтожить";
                 }
                 else if( MouseOverSlot == Drop )
                 {
                     Info.Text += "Выбросить";
                 }
                 else if( MouseOverSlot == MainWeapon )
                 {
                     Info.Text += "Основное оружиеn-----n";
                 }
                 else if( MouseOverSlot == Pistol )
                 {
                     Info.Text += "Пистолетn-----n";
                 }
                 if( MouseOverSlot.Index != -1 )
                 {
                     if( MouseOverSlot.Slot == ItemSlot.MainWeapon && MainWeapon.Index != -1 && MouseOverSlot != MainWeapon )
                     {
                         Info.Text += "Снаряжено: " + PlayerChar.Type.Items[ MainWeapon.Index ].Hint + "n-----n";
                     }
                     else if( MouseOverSlot.Slot == ItemSlot.Pistol && Pistol.Index != -1 && MouseOverSlot != Pistol )
                     {
                         Info.Text += "Снаряжено: " + PlayerChar.Type.Items[ Pistol.Index ].Hint + "n-----n";
                     }
                     Info.Text += PlayerChar.Type.Items[ MouseOverSlot.Index ].Hint;
                 }
             }
             else
             {
                 Info.Text = "";
             }
         }
   //Функция вызываемая при движении курсора
         protected override bool OnMouseMove()
         {
    //Проверяются все слоты и если курсор над какимто слотом,
    //то сохраняется ссылка на этот слот
             MouseOverSlot = null;
             for( int i = 0; i < PlayerChar.InventorySize; i++ )
             {
                 if( IsMouseOverSlot( Slots[ i ] ) )
                 {
                     MouseOverSlot = Slots[ i ];
                     break;
                 }
             }
             if( MouseOverSlot == null )
             {
                 if( IsMouseOverSlot( MainWeapon ) )
                 {
                     MouseOverSlot = MainWeapon;
                 }
                 else if( IsMouseOverSlot( Pistol ) )
                 {
                     MouseOverSlot = Pistol;
                 }
                 else if( IsMouseOverSlot( Kill ) )
                 {
                     MouseOverSlot = Kill;
                 }
                 else if( IsMouseOverSlot( Drop ) )
                 {
                     MouseOverSlot = Drop;
                 }
             }
 
             return base.OnMouseMove();
         }
   //Возвращает ссылку на персонажа игрока
         Unit GetPlayerUnit()
         {
             if( PlayerIntellect.Instance == null )
             {
                 return null;
             }
             return PlayerIntellect.Instance.ControlledObject;
         }
   //Возвращает координаты курсора
         Vec2 GetMousePos()
         {
    //Координаты переводятся в удобный формат
             return new Vec2( Inventory.MousePosition.X * Inventory.Size.Value.X, Inventory.MousePosition.Y * Inventory.Size.Value.Y );
         }
   //Возвращает ссылку на слот по его названию
         ESlot GetSlot( string Name )
         {
    //Проверяются все слоты и сравнивается имя кнопки слота
             for( int i = 0; i < PlayerChar.InventorySize; i++ )
             {
                 if( Name == Slots[ i ].Button.Name )
                 {
                     return Slots[ i ];
                 }
             }
             if( Name == MainWeapon.Button.Name )
             {
                 return MainWeapon;
             }
             if( Name == Pistol.Button.Name )
             {
                 return Pistol;
             }
             if( Name == Drop.Button.Name )
             {
                 return Drop;
             }
             if( Name == Kill.Button.Name )
             {
                 return Kill;
             }
 
             return null;
         }
   //Возвращает номер слота по сслыке на слот
         int GetSlotIndex( ESlot Slot )
         {
             for( int i = 0; i < PlayerChar.InventorySize; i++ )
             {
                 if( Slot == Slots[ i ] )
                 {
                     return i;
                 }
             }
             return -1;
         }
   //Возвращает true - если файл существует, иначе - false
         bool FileExists( string FileName )
         {
             if( !string.IsNullOrEmpty( FileName ) )
             {
                 return VirtualFile.Exists( FileName );
             }
 
             return false;
         }
   //Возвращает true - если курсор находится над слотом, иначе - false
         bool IsMouseOverSlot( ESlot Slot )
         {
             Vec2 Mouse = GetMousePos();
             return Mouse.X >= Slot.Button.Position.Value.X &&
                 Mouse.X <= Slot.Button.Position.Value.X + Slot.Button.Size.Value.X &&
                 Mouse.Y >= Slot.Button.Position.Value.Y &&
                 Mouse.Y <= Slot.Button.Position.Value.Y + Slot.Button.Size.Value.Y;
         }
   //Меняет местами Slot1 и Slot2 если это позволяет логика игры
         void Swap( ESlot Slot1, ESlot Slot2 )
         {
             if( Slot1 == Slot2 )
             {
                 return;
             }
             if( Slot1 == MainWeapon )
             {
                 PlayerCharacter.InventoryItem.Swap( PlayerChar.MainWeapon, PlayerChar.Items[ GetSlotIndex( Slot2 ) ] );
             }
             else if( Slot1 == Pistol )
             {
                 PlayerCharacter.InventoryItem.Swap( PlayerChar.Pistol, PlayerChar.Items[ GetSlotIndex( Slot2 ) ] );
             }
             else if( Slot2 == MainWeapon )
             {
                 PlayerCharacter.InventoryItem.Swap( PlayerChar.MainWeapon, PlayerChar.Items[ GetSlotIndex( Slot1 ) ] );
             }
             else if( Slot2 == Pistol )
             {
                 PlayerCharacter.InventoryItem.Swap( PlayerChar.Pistol, PlayerChar.Items[ GetSlotIndex( Slot1 ) ] );
             }
             else
             {
                 PlayerCharacter.InventoryItem.Swap( PlayerChar.Items[ GetSlotIndex( Slot1 ) ], PlayerChar.Items[ GetSlotIndex( Slot2 ) ] );
             }
             ESlot.Swap( Slot1, Slot2 );
         }
   //Функция уничтожения предмета
         void KillItem( ESlot Slot )
         {
    //Если выбрасывается предмет из слота основного оружия
             if( Slot == MainWeapon )
             {
                 if( PlayerChar.ActiveSlot == ItemSlot.MainWeapon )
                 {
                     PlayerChar.SetActiveWeapon( -1 );
                 }
                 PlayerChar.ActiveMainWeapon = -1;
                 PlayerChar.MainWeapon.Index = -1;
             }
    //Если выбрасывается предмет из слота пистолета
             else if( Slot == Pistol )
             {
                 if( PlayerChar.ActiveSlot == ItemSlot.Pistol )
                 {
                     PlayerChar.SetActiveWeapon( -1 );
                 }
                 PlayerChar.ActivePistol = -1;
                 PlayerChar.Pistol.Index = -1;
             }
    //Если выбрасывается предмет из обычного слота
             else
             {
                 PlayerChar.Items[ GetSlotIndex( Slot ) ].Index = -1;
             }
             Slot.Index = -1;
             Slot.Icon.BackTexture = null;
         }
   //Функция выброса предмета
         void DropItem( ESlot Slot )
         {
    //Создание предмета перед игроком
             Item DropedItem = (Item)Entities.Instance.Create( PlayerChar.Type.Items[ Slot.Index ].ItemType, PlayerChar.Parent );
             DropedItem.Position = new Vec3( PlayerChar.Position.X + PlayerChar.Rotation.ToMat3().Item0.X * 1.5f,
                 PlayerChar.Position.Y + PlayerChar.Rotation.ToMat3().Item0.Y * 1.5f,
                 PlayerChar.Position.Z );
             DropedItem.PostCreate();
 
             BulletItem Bullets = DropedItem as BulletItem;
    //Если выбрасываемый предмет может хранить патроны
             if( Bullets != null )
             {
     //Если выбрасывается оружие
                 if( Slot.Slot != ItemSlot.Ammo )
                 {
                     Bullets.Type.BulletCount = 0;
                     Bullets.Type.BulletCount2 = 0;
                 }
     //Если выбрасываются патроны
                 else
                 {
                     Bullets.Type.BulletCount = PlayerChar.Items[ GetSlotIndex( Slot ) ].Count;
 
                     int gunIndex = PlayerChar.GetBulletGun( Slot.Index );
                     if( gunIndex != -1 )
                     {
                         PlayerChar.Type.Items[ gunIndex ].NormalBulletCount = 0;
 
                         if( PlayerChar.ActiveWeapon != null && PlayerChar.ActiveWeapon.Type == PlayerChar.Type.Items[ gunIndex ].Type )
                         {
                             Gun gun = PlayerChar.ActiveWeapon as Gun;
                             if( gun != null )
                             {
                                 gun.NormalMode.BulletCount = 0;
                                 gun.NormalMode.BulletMagazineCount = 0;
                             }
                         }
                     }
 
                     PlayerChar.Items[ GetSlotIndex( Slot ) ].Count = 0;
                 }
             }
    //Уничтожение предмета в инвентаре
             KillItem( Slot );
         }
   //Восстановление жизней у игрока, при использовании аптечки
         void Heal( ESlot Slot )
        { 
             HealthItemType HealthItem = PlayerChar.Type.Items[ Slot.Index ].Type as HealthItemType;
             if( HealthItem != null )
             {
                 PlayerChar.SoundPlay3D( HealthItem.SoundTake, .5f, true );
 
                 float lifeMax = PlayerChar.Type.LifeMax;
 
                 if( PlayerChar.Life < lifeMax )
                 {
                     float life = PlayerChar.Life + HealthItem.Health;
                     if( life > lifeMax )
                         life = lifeMax;
 
                     PlayerChar.Life = life;
                 }
             }
         }
   //Функция вызываемая при двойном щелчке по слоту - использование
         void UseSlot( ESlot Slot )
         {
    //Если использована аптечка
             if( Slot.Slot == ItemSlot.Health )
             {
     //Восполнить жизни и уничтожить предмет
                 Heal( Slot );
                 KillItem( Slot );
             }
    //Если тип используемого предмета - основное оружие
             else if( Slot.Slot == ItemSlot.MainWeapon )
             {
     //Установить предмет как активное основное оружие
                 Swap( MainWeapon, Slot );
 
                 if( MainWeapon.Slot == PlayerChar.ActiveSlot )
                 {
                     PlayerChar.SetActiveWeapon( MainWeapon.Index );
                 }
                 PlayerChar.ActiveMainWeapon = MainWeapon.Index;
             }
    //Если тип используемого предмета - пистолет
             else if( Slot.Slot == ItemSlot.Pistol )
             {
     //Установить предмет как активный пистолет
                 Swap( Pistol, Slot );
 
                 if( Pistol.Slot == PlayerChar.ActiveSlot )
                 {
                     PlayerChar.SetActiveWeapon( Pistol.Index );
                 }
                 PlayerChar.ActivePistol = Pistol.Index;
             }
         }
   //Функция-обработчик нажатия на слот
         void Slot_Click( EButton Sender )
         {
             ESlot Target = GetSlot( Sender.Name );
    //Если нажали на слот
             if( Target != null )
             {
     //Если в данный момент не перетаскивается предмет
                 if( DragItem == null )
                 {
      //Данный предмет становится перетаскиваемым и меняется курсор
                     if( !Target.IsFree && Target != Drop && Target != Kill )
                     {
                         DragItem = Target;
                         DragItem.Button.Active = true;
                         ScreenControlManager.Instance.DefaultCursor = DragItem.Icon.BackTexture.Name;
                     }
                     FirstClickTime = Time;
                 }
     //Если другой предмет уже перетаскивается
                 else
                 {
      //Если время между двумя нажатиями на один слот меньше Delay
                     if( Target == DragItem && Time - FirstClickTime < Delay )
                     {
       //Использовать предмет
                         UseSlot( DragItem );
                     }
      //Если предмет перетащили на слот выброса
                     else if( Target == Drop )
                     {
       //Выбросить предмет
                         DropItem( DragItem );
                     }
      //Если предмет перетащили на слот уничтожения
                     else if( Target == Kill )
                     {
       //Уничтожить предмет
                         KillItem( DragItem );
                     }
      //Если предмет из обычного слота перетащили на слот основного оружия или наоборот
      //и тип предмета подходит
                     else if( DragItem == MainWeapon && ( Target.Slot == ItemSlot.MainWeapon || Target.IsFree && Target != Pistol ) ||
                         Target == MainWeapon && DragItem.Slot == ItemSlot.MainWeapon )
                     {
                         Swap( DragItem, Target );
 
                         if( MainWeapon.Slot == PlayerChar.ActiveSlot || MainWeapon.Slot == ItemSlot.None )
                         {
                             PlayerChar.SetActiveWeapon( MainWeapon.Index );
                         }
                         PlayerChar.ActiveMainWeapon = MainWeapon.Index;
                     }
      //Если предмет из обычного слота перетащили на слот пистолета или наоборот
      //и тип предмета подходит
                     else if( DragItem == Pistol && ( Target.Slot == ItemSlot.Pistol || Target.IsFree && Target != MainWeapon ) ||
                         Target == Pistol && DragItem.Slot == ItemSlot.Pistol )
                     {
                         Swap( DragItem, Target );
 
                         if( Pistol.Slot == PlayerChar.ActiveSlot || Pistol.Slot == ItemSlot.None )
                         {
                             PlayerChar.SetActiveWeapon( Pistol.Index );
                         }
                         PlayerChar.ActivePistol = Pistol.Index;
                     }
      //Если предмет из обычного слота перетащили на другой обычный слот
                     else if( DragItem != MainWeapon && DragItem != Pistol && Target != MainWeapon && Target != Pistol )
                     {
                         Swap( DragItem, Target );
                     }
      //Курсор меняется обратно
                     ScreenControlManager.Instance.DefaultCursor = "Cursors\\Default.png";
                     DragItem.Button.Active = false;
                     DragItem = null;
                 }
             }
         }
     }
 }