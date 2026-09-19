using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BrothersBlock
{
    public sealed class GameHud : MonoBehaviour
    {
        public static GameHud Instance { get; private set; }
        public Vector2 Movement { get { return stick == null ? Vector2.zero : stick.Value; } }
        public bool Running { get { return run != null && run.Held; } }
        private RectTransform safe;
        private GameObject menu, playing;
        private InputField address;
        private Text status, room, hero, objective, toast, character, abilities, modeDescription;
        private Button hostButton, joinButton, cancelButton, skinButton, hairButton, clothButton;
        private Button[] presetButtons = new Button[9], elementButtons = new Button[5], modeButtons = new Button[4], skillButtons = new Button[4];
        private Text[] skillLabels = new Text[4];
        private Image healthFill;
        private MoveStick stick;
        private LookPad look;
        private HoldButton run;
        private bool jump;
        private float toastUntil;
        private string toastWords;
        private Font font;
        private LanSession session;
        private Sprite rounded;
        private Texture2D roundedTexture;
        private readonly Color ink = new Color(.055f,.10f,.14f,.96f), accent = new Color(.95f,.76f,.40f), muted = new Color(.70f,.78f,.81f), buttonColour = new Color(.16f,.24f,.29f);
        private void Start()
        {
            Instance=this; session=LanSession.Instance; font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); rounded=MakeRoundedSprite();
            var canvas=gameObject.AddComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            if (EventSystem.current==null) { var events=new GameObject("UI events"); events.AddComponent<EventSystem>(); events.AddComponent<StandaloneInputModule>(); }
            safe=Rect("Safe area",transform); Stretch(safe); BuildMenu(); BuildPlayingHud(); session.Changed+=Refresh; Refresh();
        }
        private void BuildMenu()
        {
            RectTransform root=Rect("Choose your journey",safe); Stretch(root); menu=root.gameObject; root.gameObject.AddComponent<Image>().color=new Color(.02f,.06f,.09f,.66f);
            Label("AVATAR  /  A TWO-PLAYER FAN ADVENTURE",root,new Vector2(1200,28),new Vector2(0,413),16,accent);
            Label("ELEMENTAL JOURNEY",root,new Vector2(1300,64),new Vector2(0,360),49,Color.white,FontStyle.Bold);
            Label("Choose your path. Restore balance together.",root,new Vector2(1200,35),new Vector2(0,309),21,muted);
            RectTransform left=Panel("Character selection",root,new Vector2(690,585),new Vector2(-350,-20),ink);
            RectTransform right=Panel("Choose a mode",root,new Vector2(630,585),new Vector2(340,-20),ink);
            character=Label("",left,new Vector2(620,46),new Vector2(0,247),28,Color.white,FontStyle.Bold);
            for (int i=1;i<=8;i++) { int chosen=i; presetButtons[i]=Button(BendingRules.Names[i],left,new Vector2(144,43),new Vector2(-231+((i-1)%4)*154,185-((i-1)/4)*51),buttonColour,Color.white,()=>{ JourneyProfile.Select(chosen); RefreshChoices(); }); }
            presetButtons[0]=Button("CUSTOM CHARACTER",left,new Vector2(606,40),new Vector2(0,77),buttonColour,Color.white,()=>{ JourneyProfile.Preset=0; RefreshChoices(); });
            for (int i=0;i<5;i++) { int chosen=i; elementButtons[i]=Button(i==4?"NON-BENDER":((Element)i).ToString().ToUpperInvariant(),left,new Vector2(115,46),new Vector2(-244+i*122,17),buttonColour,BendingRules.Colours[i],()=>{ JourneyProfile.Preset=0; JourneyProfile.Kind=(Element)chosen; RefreshChoices(); }); }
            elementButtons[4].GetComponentInChildren<Text>().fontSize=14;
            skinButton=Button("",left,new Vector2(195,42),new Vector2(-204,-44),buttonColour,Color.white,()=>{ JourneyProfile.Skin=(JourneyProfile.Skin+1)%5; RefreshChoices(); });
            hairButton=Button("",left,new Vector2(195,42),new Vector2(0,-44),buttonColour,Color.white,()=>{ JourneyProfile.Hair=(JourneyProfile.Hair+1)%3; RefreshChoices(); });
            clothButton=Button("",left,new Vector2(195,42),new Vector2(204,-44),buttonColour,Color.white,()=>{ JourneyProfile.Outfit=(JourneyProfile.Outfit+1)%6; RefreshChoices(); });
            abilities=Label("",left,new Vector2(610,182),new Vector2(0,-167),19,muted); abilities.alignment=TextAnchor.UpperLeft;
            Label("CHOOSE A JOURNEY",right,new Vector2(570,40),new Vector2(0,247),22,accent,FontStyle.Bold);
            for (int i=0;i<4;i++) { int chosen=i; modeButtons[i]=Button(BendingRules.ModeNames[i],right,new Vector2(270,50),new Vector2(i%2==0?-143:143,181-(i/2)*62),buttonColour,Color.white,()=>{ JourneyProfile.Mode=(JourneyMode)chosen; RefreshChoices(); }); }
            modeDescription=Label("",right,new Vector2(553,92),new Vector2(0,32),19,muted);
            hostButton=Button("PLAY / HOST",right,new Vector2(554,58),new Vector2(0,-52),accent,ink,()=>session.Host());
            RectTransform field=Panel("Host address",right,new Vector2(405,48),new Vector2(-75,-119),buttonColour);
            address=field.gameObject.AddComponent<InputField>(); Text input=Label("",field,new Vector2(380,44),Vector2.zero,22,Color.white); input.alignment=TextAnchor.MiddleLeft;
            Text placeholder=Label("Brother's Wi-Fi address",field,new Vector2(380,44),Vector2.zero,19,muted); placeholder.alignment=TextAnchor.MiddleLeft;
            address.textComponent=input; address.placeholder=placeholder; address.characterLimit=15; address.keyboardType=TouchScreenKeyboardType.NumbersAndPunctuation;
            joinButton=Button("JOIN",right,new Vector2(135,48),new Vector2(210,-119),buttonColour,Color.white,()=>session.Join(address.text));
            status=Label("",right,new Vector2(550,85),new Vector2(0,-190),18,muted);
            cancelButton=Button("CANCEL",right,new Vector2(160,35),new Vector2(0,-260),buttonColour,Color.white,()=>session.Leave());
            Label("SAME WI-FI  /  1-2 PLAYERS  /  PROGRESS SAVES ON EACH PHONE",root,new Vector2(1350,32),new Vector2(0,-358),17,Color.white);
            Label("A short original chapter and stylized character interpretations. Host chooses the room's mode.",root,new Vector2(1350,32),new Vector2(0,-397),15,muted);
        }
        private void RefreshChoices()
        {
            int xp=JourneyProfile.XP(JourneyProfile.Kind);
            character.text=BendingRules.Names[JourneyProfile.Preset]+"  /  "+JourneyProfile.Kind.ToString().ToUpperInvariant();
            abilities.text=BendingRules.Specialty(JourneyProfile.Kind)+"\n\nLEVEL "+BendingRules.Level(xp)+"  |  "+xp+" XP\n";
            for (int slot=0;slot<4;slot++) abilities.text+=(slot==0?"": "   ")+"Lv "+BendingRules.UnlockLevels[slot]+": "+BendingRules.Ability(JourneyProfile.Kind,slot,JourneyProfile.Preset)+(slot%2==1?"\n":"");
            modeDescription.text=BendingRules.ModeDescriptions[(int)JourneyProfile.Mode];
            for (int i=0;i<9;i++) presetButtons[i].GetComponent<Image>().color=JourneyProfile.Preset==i?new Color(.32f,.42f,.43f):buttonColour;
            for (int i=0;i<5;i++) elementButtons[i].GetComponent<Image>().color=(int)JourneyProfile.Kind==i?new Color(.30f,.37f,.38f):buttonColour;
            for (int i=0;i<4;i++) modeButtons[i].GetComponent<Image>().color=(int)JourneyProfile.Mode==i?new Color(.38f,.32f,.22f):buttonColour;
            skinButton.GetComponentInChildren<Text>().text="SKIN  "+(JourneyProfile.Skin+1)+" / 5";
            skinButton.GetComponent<Image>().color=BendingRules.Skin[JourneyProfile.Skin]*.6f;
            hairButton.GetComponentInChildren<Text>().text="HAIR  "+new[]{"SHORT","TOPKNOT","BALD"}[JourneyProfile.Hair];
            clothButton.GetComponentInChildren<Text>().text="OUTFIT  "+(JourneyProfile.Outfit+1)+" / 6";
            clothButton.GetComponent<Image>().color=BendingRules.Cloth[JourneyProfile.Outfit]*.8f;
            PlayerPrefs.Save();
        }
        private void BuildPlayingHud()
        {
            RectTransform root=Rect("In game",safe); Stretch(root); playing=root.gameObject;
            RectTransform area=Rect("Drag to look",root); area.anchorMin=new Vector2(.35f,0); area.anchorMax=Vector2.one; area.offsetMin=area.offsetMax=Vector2.zero; area.gameObject.AddComponent<Image>().color=Color.clear; look=area.gameObject.AddComponent<LookPad>();
            RectTransform stats=Panel("Hero status",root,new Vector2(430,107),new Vector2(236,-74),ink,new Vector2(0,1));
            hero=Label("",stats,new Vector2(390,65),new Vector2(0,10),18,Color.white); hero.alignment=TextAnchor.MiddleLeft;
            RectTransform bar=Panel("Health bar",stats,new Vector2(390,7),new Vector2(0,-37),buttonColour); RectTransform fill=Rect("Health",bar); Stretch(fill); healthFill=fill.gameObject.AddComponent<Image>(); healthFill.color=new Color(.45f,.78f,.51f);
            room=Label("",Panel("Room",root,new Vector2(780,70),new Vector2(60,-56),ink,new Vector2(.5f,1)),new Vector2(740,60),Vector2.zero,17,Color.white);
            Button("LEAVE",root,new Vector2(125,50),new Vector2(-88,-50),ink,Color.white,()=>session.Leave(),new Vector2(1,1));
            objective=Label("",Panel("Objective",root,new Vector2(1210,74),new Vector2(0,-172),ink,new Vector2(.5f,1)),new Vector2(1165,66),Vector2.zero,20,Color.white);
            toast=Label("",root,new Vector2(1070,80),new Vector2(0,-272),21,accent,FontStyle.Bold,new Vector2(.5f,1));
            Label("+",root,new Vector2(40,40),Vector2.zero,25,new Color(1,1,1,.65f));
            RectTransform stickBase=Panel("Move",root,new Vector2(180,180),new Vector2(146,150),new Color(.05f,.10f,.14f,.6f),Vector2.zero);
            stick=stickBase.gameObject.AddComponent<MoveStick>(); stick.Knob=Panel("Thumb",stickBase,new Vector2(78,78),Vector2.zero,new Color(.88f,.91f,.82f,.82f)); stick.Knob.GetComponent<Image>().raycastTarget=false;
            Label("MOVE",root,new Vector2(160,30),new Vector2(146,40),16,Color.white,FontStyle.Normal,Vector2.zero);
            for (int i=0;i<4;i++) {
                int slot=i; Vector2 size=i==0?new Vector2(132,116):new Vector2(164,66); Vector2 position=i==0?new Vector2(-103,157):new Vector2(-272,252-(i-1)*82);
                skillButtons[i]=Button("",root,size,position,i==0?accent:ink,i==0?ink:Color.white,()=>{ if (BenderCombat.Local!=null) BenderCombat.Local.TryCast(slot); },new Vector2(1,0));
                skillLabels[i]=skillButtons[i].GetComponentInChildren<Text>();
            }
            Button("JUMP",root,new Vector2(115,63),new Vector2(-103,275),ink,Color.white,()=>jump=true,new Vector2(1,0));
            run=Button("RUN",root,new Vector2(115,54),new Vector2(-103,56),ink,Color.white,()=>{},new Vector2(1,0)).gameObject.AddComponent<HoldButton>();
            Button("INTERACT",root,new Vector2(153,52),new Vector2(-113,361),new Color(.19f,.35f,.39f),Color.white,()=>{ if(BenderCombat.Local!=null) BenderCombat.Local.Interact(); },new Vector2(1,0));
            Label(Application.isMobilePlatform?"FACE AN ENEMY TO AIM  /  DRAG TO LOOK":"WASD move  /  F attack  /  1 2 3 skills  /  E interact  /  Right mouse look",root,new Vector2(890,36),new Vector2(-20,28),14,Color.white,FontStyle.Normal,new Vector2(.5f,0));
        }
        private void Update()
        {
            if (safe==null) return; Rect bounds=Screen.safeArea;
            safe.anchorMin=new Vector2(bounds.xMin/Screen.width,bounds.yMin/Screen.height); safe.anchorMax=new Vector2(bounds.xMax/Screen.width,bounds.yMax/Screen.height);
            if (Input.GetKeyDown(KeyCode.Escape)&&(session.Playing||session.Busy)) session.Leave();
            if (playing.activeSelf!=session.Playing) Refresh();
            if (!session.Playing) return;
            BenderCombat player=BenderCombat.Local; AdventureDirector director=AdventureDirector.Instance;
            if (player!=null) {
                hero.text=BendingRules.Names[player.Preset.Value]+"  |  LEVEL "+BendingRules.Level(player.XP.Value)+"\n"+(player.Down?"DOWN - revive or respawn shortly":player.HP.Value+" HP   "+player.Shield.Value+" GUARD")+"  |  "+player.XP.Value+" / "+BendingRules.NextXP(player.XP.Value)+" XP";
                healthFill.rectTransform.anchorMax=new Vector2(player.HP.Value/100f,1);
                healthFill.color=Color.Lerp(new Color(.85f,.25f,.20f),new Color(.40f,.78f,.49f),player.HP.Value/100f);
                for(int slot=0;slot<4;slot++) {
                    bool unlocked=BendingRules.Unlocked(player.XP.Value,slot); float cd=player.Cooldown(slot);
                    skillLabels[slot].text=BendingRules.Ability(player.Element,slot,player.Preset.Value).ToUpperInvariant()+"\n"+(!unlocked?"LEVEL "+BendingRules.UnlockLevels[slot]:cd>.05f?cd.ToString("0.0")+"s":"READY");
                    skillButtons[slot].interactable=unlocked&&cd<=0&&!player.Down&&!player.Stunned;
                }
            }
            if (director!=null) {
                room.text=BendingRules.ModeNames[director.State.mode]+"  /  "+(session.Manager.IsHost?session.Players+" / 2 PLAYERS":"CONNECTED TO YOUR BROTHER")+"\n"+(session.Manager.IsHost?"Host address: "+session.HostAddress:"Host chooses the mode. Your character and XP stay yours.");
                objective.text=director.State.objective;
                if (director.State.showMarker&&player!=null) objective.text+="  ["+Mathf.RoundToInt(Vector3.Distance(player.transform.position,director.State.marker))+" m]";
                if (director.Mode==JourneyMode.Duel) { foreach(BenderCombat p in AdventureDirector.Players()) objective.text+="  |  "+BendingRules.Names[p.Preset.Value]+": "+p.Score.Value; }
            }
            toast.text=Time.unscaledTime<toastUntil?toastWords:"";
        }
        public void ShowToast(string message) { toastWords=message; toastUntil=Time.unscaledTime+6; }
        private void Refresh() { menu.SetActive(!session.Playing); playing.SetActive(session.Playing); status.text=session.Message; hostButton.interactable=joinButton.interactable=address.interactable=!session.Busy; cancelButton.gameObject.SetActive(session.Busy); if(!session.Playing){jump=false;RefreshChoices();} }
        public bool ConsumeJump() { bool value=jump; jump=false; return value; }
        public Vector2 ConsumeLook() { return look==null?Vector2.zero:look.Consume(); }
        private RectTransform Rect(string name,Transform parent) { var item=new GameObject(name,typeof(RectTransform)); item.transform.SetParent(parent,false); return (RectTransform)item.transform; }
        private void Stretch(RectTransform rect) { rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero; }
        private RectTransform Panel(string name,Transform parent,Vector2 size,Vector2 position,Color colour,Vector2? anchor=null) {
            RectTransform rect=Rect(name,parent);rect.anchorMin=rect.anchorMax=anchor??new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;
            Image image=rect.gameObject.AddComponent<Image>();image.sprite=rounded;image.type=Image.Type.Sliced;image.color=colour;return rect;
        }
        private Text Label(string words,Transform parent,Vector2 size,Vector2 position,int fontSize,Color colour,FontStyle style=FontStyle.Normal,Vector2? anchor=null) {
            RectTransform rect=Rect("Label",parent);rect.anchorMin=rect.anchorMax=anchor??new Vector2(.5f,.5f);rect.sizeDelta=size;rect.anchoredPosition=position;
            Text text=rect.gameObject.AddComponent<Text>();text.font=font;text.text=words;text.fontSize=fontSize;text.fontStyle=style;text.color=colour;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;return text;
        }
        private Button Button(string words,Transform parent,Vector2 size,Vector2 position,Color background,Color foreground,UnityAction clicked,Vector2? anchor=null) {
            RectTransform rect=Panel(words,parent,size,position,background,anchor);Button button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();button.onClick.AddListener(clicked);
            Label(words,rect,size-new Vector2(10,4),Vector2.zero,18,foreground,FontStyle.Bold);return button;
        }
        private Sprite MakeRoundedSprite() {
            roundedTexture=new Texture2D(64,64,TextureFormat.RGBA32,false);roundedTexture.wrapMode=TextureWrapMode.Clamp;
            for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(Mathf.Abs(x-31.5f)-17,0),dy=Mathf.Max(Mathf.Abs(y-31.5f)-17,0);roundedTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(15-Mathf.Sqrt(dx*dx+dy*dy))));}
            roundedTexture.Apply();return Sprite.Create(roundedTexture,new Rect(0,0,64,64),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
        }
        private void OnDestroy() { if(session!=null)session.Changed-=Refresh;if(Instance==this)Instance=null;if(rounded!=null)Destroy(rounded);if(roundedTexture!=null)Destroy(roundedTexture); }
    }
}
