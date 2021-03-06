using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using System.IO;
using Munchkin.Control;
using Munchkin.Model;
using System.Runtime.Serialization.Formatters.Binary;

namespace Munchkin
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game : Microsoft.Xna.Framework.Game
    {
        #region Members
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        //Current state our game is in when started at Main Menu
        GameState gameState = GameState.Login;
        //GameState gameState = GameState.MainMenu;

        //The messages to display on the screen.
        //We will use this to show events that occur like
        //a player joining a session.
        List<DisplayMessage> gameMessages = new List<DisplayMessage>();
        List<Player> players = new List<Player>();

        Deck doorDeck;
        Deck treasureDeck;
        Deck masterDeck;

        Discard doorDiscard;
        Discard treasureDiscard;

        Discard selectedDiscard;

        static List<Card> ClickableCards = new List<Card>();

        Deck selectedDeck;
        Card focusCard;

        PlayingField playingField;

        ButtonHandler buttons;
        Button gameOverButton;

        int menuIndex, currentDieRoll, currentLevel, gameOverTracker;
        bool levelUpClicked = false, levelDownClicked = false, resetButtonClicked = false, diceButtonClicked = false, restart = false;

        SpriteFont spriteFont;
        SpriteFont textFont;
        SpriteFont levelFont;
        Background bg;
        Menu mainMenu;
        Menu sessionMenu;
        Menu loginMenu;
        Menu lobbyMenu;
        Menu findMenu;
        GamePadState currentGamePadState;
        GamePadState lastGamePadState;
        KeyboardState currentKeyState;
        KeyboardState lastKeyState;
        MouseState currentMouseState;
        MouseState lastMouseState;
        Random random = new Random();
        //The network session for the game
        NetworkSession networkSession;

        Player player;//Represent local player 1. 
        Player player2;//Represents player 2
        Player player3;//... 3
        Player player4;//... 4
        List<Player> remotePlayers;

        GameOverStatSet goss;

        //List of sessions that you can join
        AvailableNetworkSessionCollection availableSessions;

        string text = "";
        List<string> chatBoxText = new List<string>();

        Keys[] keysToCheck = new Keys[] { 
            Keys.A, Keys.B, Keys.C, Keys.D, Keys.E,
            Keys.F, Keys.G, Keys.H, Keys.I, Keys.J,
            Keys.K, Keys.L, Keys.M, Keys.N, Keys.O,
            Keys.P, Keys.Q, Keys.R, Keys.S, Keys.T,
            Keys.U, Keys.V, Keys.W, Keys.X, Keys.Y,
            Keys.Z, Keys.Back, Keys.Space, Keys.Enter,
            Keys.OemBackslash, Keys.OemComma, Keys.OemMinus, Keys.OemPlus,
        Keys.OemQuestion, Keys.OemQuotes, Keys.OemSemicolon, Keys.Subtract,
        Keys.Multiply, Keys.Divide, Keys.Decimal, Keys.Add, Keys.D0, Keys.D1,
        Keys.D2, Keys.D3, Keys.D4, Keys.D5,Keys.D6,Keys.D7,Keys.D8, Keys.D9, 
        Keys.OemOpenBrackets, Keys.OemCloseBrackets, Keys.OemPeriod, Keys.Tab};

        Texture2D chatRectangle, chatOutline, die1Texture, die2Texture, die3Texture, die4Texture, die5Texture, die6Texture,
            levelBoxTexture, levelDownButtonTexture, levelUpButtonTexture, resetButtonTexture, gameOverChecked, gameOverUnchecked;

        Rectangle diceClickArea = new Rectangle(1549, 654, 50, 50);
        Rectangle levelUpClickArea = new Rectangle(1399, 652, 25, 25);
        Rectangle levelDownClickArea = new Rectangle(1399, 679, 25, 25);
        Rectangle resetLifeClickArea = new Rectangle(1440, 652, 50, 50);
        Rectangle gameOverClickArea = new Rectangle(1572, 12, 20, 20);

        //Textbox and functionality variables for Logging in. 
        //username, password text boxes. And outlines to show
        //the active box since there is no blinking cursor.
        Rectangle userNameBox, userNameBoxOutline, passwordBox, passwordBoxOutline, loginBtn, cancelBtn, createBtn;
        bool usernameActive, passwordActive, gameOver, gameOverStatsPulled;
        int loginAttempt; //Stores login attempts.
        string createResult; //Stores result of account creation
        string pw = ""; //stores players password
        string un = ""; //stores players username
        string pwMask = ""; //stores a mask for the players password. Will be filled with *
        Texture2D texLogin, texCancel, texCreate;
        List<Deck> initialDecks;
        #endregion Members

        #region InitializationMethods

        /// <summary>
        /// Sets up an instance of the game.
        /// </summary>
        public Game()
        {

            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1600;
            graphics.PreferredBackBufferHeight = 960;
            Content.RootDirectory = "Content";

            //Initialize GamerServices

            Components.Add(new GamerServicesComponent(this));
        }

        /// <summary>
        /// Sets up an enumerated GameState.
        /// </summary>
        public enum GameState { Login, MainMenu, CreateSession, FindSession, GameLobby, PlayingGame, GameOver };

        /// <summary>
        /// Sets up an enumerated GameType
        /// </summary>
        public enum GameType { Match };

        /// <summary>
        /// Sets up an enumerated list of session properties
        /// </summary>
        public enum SessionProperties { GameType, OtherCustomProperty };

        /// <summary>
        /// Structure used to display game messages.
        /// </summary>
        public struct DisplayMessage
        {
            public string Message;
            public TimeSpan DisplayTime;

            public DisplayMessage(string message, TimeSpan displayTime)
            {
                Message = message;
                DisplayTime = displayTime;
            }
        }


        /// <summary>
        /// Sets up all of the instances of this game
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            bg = new Background();
            mainMenu = new Menu();
            sessionMenu = new Menu();
            loginMenu = new Menu();
            lobbyMenu = new Menu();
            findMenu = new Menu();
            player = new Player();
            currentKeyState = new KeyboardState();
            currentMouseState = Mouse.GetState();
            base.Initialize();
            
            loginAttempt = 0;
            createResult = "";

            usernameActive = true;
            passwordActive = false;
            this.Reinitialize();
        }

        /// <summary>
        /// Sets up all of the instances again when the game is restarted
        /// </summary>
        private void Reinitialize()
        {
            playingField = new PlayingField();      
            doorDiscard = new Discard(new Vector2(670, 350));
            treasureDiscard = new Discard(new Vector2(770, 350));
            gameOver = false;
            gameOverStatsPulled = false;
            menuIndex = 0;
            currentDieRoll = 6;
            currentLevel = 1;
            gameOverTracker = 0;
            gameOverStatsPulled = false;
            chatBoxText = new List<string>();
            initialDecks = CardController.CreateDeck(graphics.GraphicsDevice);
            players = new List<Player>();
            foreach(Player p in players)
            {
                p.Hand.Clear();
                p.Backpack.Clear();
                p.Equipped.Clear();
                p.Level = 1;
            }
            doorDeck = initialDecks[0];
            doorDeck.Shuffle();
            treasureDeck = initialDecks[1];
            treasureDeck.Shuffle();
            masterDeck = initialDecks[2];
            restart = false;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            spriteFont = Content.Load<SpriteFont>("Fonts//SpriteFont1");
            textFont = Content.Load<SpriteFont>("Fonts//SpriteFont2");
            levelFont = Content.Load<SpriteFont>("Fonts//SpriteFont3");
            //Felt Background image retrieved from: http://blinds-wallpaper.net/wallpaper/CONTACT-PAPER/VELOUR-FELT/Green-Billiards-Velour-Felt-Contact-Paper/prod_13816.html
            bg.Initialize(Content.Load<Texture2D>("UI//felt_bg"), new Vector2(0, 0), this.GraphicsDevice.Viewport.Width, this.GraphicsDevice.Viewport.Height);

            mainMenu.Initialize(Content.Load<Texture2D>("Menus//Main//main_menu"), new Vector2(400, 80));
            mainMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Main//create_a_session"), new Vector2(mainMenu.StartPosition.X + 200, mainMenu.StartPosition.Y + 300), "Create a Session", true));
            mainMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Main//find_a_session"), new Vector2(mainMenu.StartPosition.X + 200, mainMenu.StartPosition.Y + 401), "Find a Session", true));
            mainMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Main//exit"), new Vector2(mainMenu.StartPosition.X + 200, mainMenu.StartPosition.Y + 501), "Exit", true));

            sessionMenu.Initialize(Content.Load<Texture2D>("Menus//Session//session_menu"), new Vector2(400, 80));
            sessionMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Session//create_a_match"), new Vector2(sessionMenu.StartPosition.X + 200, sessionMenu.StartPosition.Y + 300), "Create a Match", true));
            sessionMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Session//back"), new Vector2(sessionMenu.StartPosition.X + 200, sessionMenu.StartPosition.Y + 401), "Back", true));

            loginMenu.Initialize(Content.Load<Texture2D>("Menus//Login//login_menu"), new Vector2(400, 80));
            loginMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Login//login"), new Vector2(loginMenu.StartPosition.X + 265, loginMenu.StartPosition.Y + 500), "Login", true));
            loginMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Login//cancel"), new Vector2(loginMenu.StartPosition.X + 450, loginMenu.StartPosition.Y + 500), "Cancel", true));
            loginMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Login//create"), new Vector2(loginMenu.StartPosition.X + 305, loginMenu.StartPosition.Y + 575), "Create", true));

            lobbyMenu.Initialize(Content.Load<Texture2D>("Menus//Lobby//lobby_menu"), new Vector2(200, 80));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_playercard"), new Vector2(lobbyMenu.StartPosition.X + 50, lobbyMenu.StartPosition.Y + 50), "Playercard 1", true));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_playercard"), new Vector2(lobbyMenu.StartPosition.X + 50, lobbyMenu.StartPosition.Y + 200), "Playercard 2", true));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_playercard"), new Vector2(lobbyMenu.StartPosition.X + 50, lobbyMenu.StartPosition.Y + 350), "Playercard 3", true));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_playercard"), new Vector2(lobbyMenu.StartPosition.X + 50, lobbyMenu.StartPosition.Y + 500), "Playercard 4", true));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_menubox"), new Vector2(lobbyMenu.StartPosition.X + 625, lobbyMenu.StartPosition.Y + 650), "Menubox", true));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_checkbox"), new Vector2(lobbyMenu["Playercard 1"].StartPosition.X + 990, lobbyMenu["Playercard 1"].StartPosition.Y + 30), "PlayerReady 1", false));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_checkbox"), new Vector2(lobbyMenu["Playercard 2"].StartPosition.X + 990, lobbyMenu["Playercard 2"].StartPosition.Y + 30), "PlayerReady 2", false));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_checkbox"), new Vector2(lobbyMenu["Playercard 3"].StartPosition.X + 990, lobbyMenu["Playercard 3"].StartPosition.Y + 30), "PlayerReady 3", false));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_checkbox"), new Vector2(lobbyMenu["Playercard 4"].StartPosition.X + 990, lobbyMenu["Playercard 4"].StartPosition.Y + 30), "PlayerReady 4", false));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_start"), new Vector2(lobbyMenu["Menubox"].StartPosition.X + 305, lobbyMenu["Menubox"].StartPosition.Y + 24), "Can Start", false));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_cantstart"), new Vector2(lobbyMenu["Menubox"].StartPosition.X + 305, lobbyMenu["Menubox"].StartPosition.Y + 24), "Cant Start", false));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_back"), new Vector2(lobbyMenu["Menubox"].StartPosition.X + 70, lobbyMenu["Menubox"].StartPosition.Y + 24), "Host Back", false));
            lobbyMenu.Add(new Asset(Content.Load<Texture2D>("Menus//Lobby//lobby_menu_back"), new Vector2(lobbyMenu["Menubox"].StartPosition.X + 180, lobbyMenu["Menubox"].StartPosition.Y + 24), "Guest Back", false));

            findMenu.Initialize(Content.Load<Texture2D>("Menus//Find//find_menu"), new Vector2(200, 80));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//game_card"), new Vector2(findMenu.StartPosition.X + 50, findMenu.StartPosition.Y + 50), "Gamecard 1", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//game_card"), new Vector2(findMenu.StartPosition.X + 50, findMenu.StartPosition.Y + 125), "Gamecard 2", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//game_card"), new Vector2(findMenu.StartPosition.X + 50, findMenu.StartPosition.Y + 200), "Gamecard 3", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//game_card"), new Vector2(findMenu.StartPosition.X + 50, findMenu.StartPosition.Y + 275), "Gamecard 4", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//game_card"), new Vector2(findMenu.StartPosition.X + 50, findMenu.StartPosition.Y + 350), "Gamecard 5", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//game_card"), new Vector2(findMenu.StartPosition.X + 50, findMenu.StartPosition.Y + 425), "Gamecard 6", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//game_card"), new Vector2(findMenu.StartPosition.X + 50, findMenu.StartPosition.Y + 500), "Gamecard 7", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//game_card"), new Vector2(findMenu.StartPosition.X + 50, findMenu.StartPosition.Y + 575), "Gamecard 8", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//menubox"), new Vector2(findMenu.StartPosition.X + 338, findMenu.StartPosition.Y + 650), "Menubox", true)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//back"), new Vector2(findMenu["Menubox"].StartPosition.X + 70, findMenu["Menubox"].StartPosition.Y + 24), "Back", true)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//no_sessions"), new Vector2(findMenu.StartPosition.X + 300, findMenu.StartPosition.Y + 100), "No Sessions", false)));
            findMenu.Add((new Asset(Content.Load<Texture2D>("Menus//Find//refresh"), new Vector2(findMenu["Menubox"].StartPosition.X + 305, findMenu["Menubox"].StartPosition.Y + 24), "Refresh", true)));


            buttons = new ButtonHandler(new Vector2(0, 760));

            Button b1 = new Button(Content.Load<Texture2D>("Buttons//DrawFaceUpButton"), "Draw Face Up");
            Button b2 = new Button(Content.Load<Texture2D>("Buttons//EquipButton"), "Add To Equipped");
            Button b4 = new Button(Content.Load<Texture2D>("Buttons//ToBackButton"), "Add To BackPack");
            Button b5 = new Button(Content.Load<Texture2D>("Buttons//ToFieldButton"), "Add To Playing Field");
            Button b52 = new Button(Content.Load<Texture2D>("Buttons//ClearField"), "Clear Playing Field");
            Button b6 = new Button(Content.Load<Texture2D>("Buttons//ToHandButton"), "Add To Hand");
            Button b7 = new Button(Content.Load<Texture2D>("Buttons//Discard"), "Discard");
            gameOverButton = new Button(Content.Load<Texture2D>("Buttons//ExitButton"), "OVER");
            

            buttons.Add(b1);
            buttons.Add(b2);
            buttons.Add(b4);
            buttons.Add(b5);
            buttons.Add(b6);
            buttons.Add(b7);
            buttons.Add(b52);

            //Texture for the chatbox
            chatRectangle = new Texture2D(GraphicsDevice, 1, 1);
            chatRectangle.SetData(new[] { Color.White });
            chatOutline = new Texture2D(GraphicsDevice, 1, 1);
            chatOutline.SetData(new[] { Color.Silver });
            userNameBox = new Rectangle((int)loginMenu.StartPosition.X + 275, (int)loginMenu.StartPosition.Y + 350, 300, 25);
            passwordBox = new Rectangle((int)loginMenu.StartPosition.X + 275, (int)loginMenu.StartPosition.Y + 410, 300, 25);

            //Textures for the dice
            die1Texture = this.Content.Load<Texture2D>("Dice//dice1");
            die2Texture = this.Content.Load<Texture2D>("Dice//dice2");
            die3Texture = this.Content.Load<Texture2D>("Dice//dice3");
            die4Texture = this.Content.Load<Texture2D>("Dice//dice4");
            die5Texture = this.Content.Load<Texture2D>("Dice//dice5");
            die6Texture = this.Content.Load<Texture2D>("Dice//dice6");

            //Textures for game over checkbox
            gameOverUnchecked = this.Content.Load<Texture2D>("Menus//Lobby//lobby_menu_uncheckbox");
            gameOverChecked = this.Content.Load<Texture2D>("Menus//Lobby//lobby_menu_checkbox");

            //Textures for LevelUpBox
            levelBoxTexture = this.Content.Load<Texture2D>("LevelUpBox//LevelBox");
            levelDownButtonTexture = this.Content.Load<Texture2D>("LevelUpBox//LevelDownButton");
            levelUpButtonTexture = this.Content.Load<Texture2D>("LevelUpBox//LevelUpButton");
            resetButtonTexture = this.Content.Load<Texture2D>("LevelUpBox//ResetButton");
        }
        

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            chatRectangle.Dispose();
            chatOutline.Dispose();
        }
        #endregion InitializationMethods

        #region UpdateMethods
        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            this.IsMouseVisible = true;
            //Store the current Keyboard State
            lastKeyState = currentKeyState;
            currentKeyState = Keyboard.GetState();

            // Allows the game to exit
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
            //    this.Exit();
            //if (currentKeyState.IsKeyDown(Keys.Escape))
            //    this.Exit();

            lastMouseState = currentMouseState;
            currentMouseState = Mouse.GetState();

            //If there is no user signed in after 5 seconds
            //then we show the sign in
            if (gameTime.TotalGameTime.Seconds > 5)
            {
                if (Gamer.SignedInGamers.Count == 0 && !Guide.IsVisible)
                    Guide.ShowSignIn(1, false);
            }



            //Switch to determine which update method to call
            //based on the current game state
            switch (gameState)
            {
                case GameState.Login:
                    LoginUpdate(gameTime);
                    break;
                case GameState.MainMenu:
                    MainMenuUpdate();
                    break;
                case GameState.CreateSession:
                    CreateSessionUpdate();
                    break;
                case GameState.GameLobby:
                    GameLobbyUpdate(gameTime);
                    break;
                case GameState.PlayingGame:
                    PlayingGameUpdate(gameTime);
                    break;
                case GameState.FindSession:
                    FindSessionUpdate();
                    break;
                case GameState.GameOver:
                    GameOverUpdate(gameTime);
                    break;
            }

            //Store the current GamePadState
            currentGamePadState = GamePad.GetState(PlayerIndex.One);
            //Store the game pad state for next frame
            lastGamePadState = currentGamePadState;

            //Update the DisplayTime of current display message
            if (gameMessages.Count > 0)
            {
                DisplayMessage currentMessage = gameMessages[0];
                currentMessage.DisplayTime -= gameTime.ElapsedGameTime;

                //Remove the message if the time is up
                if (currentMessage.DisplayTime <= TimeSpan.Zero)
                {
                    gameMessages.RemoveAt(0);
                }
                else
                {
                    gameMessages[0] = currentMessage;
                }
            }

            //Update the network session we need to check to
            //see if it is disposed since calling update
            //on a displosed NetworkSession will throw an exception
            if (networkSession != null && !networkSession.IsDisposed)
            {
                networkSession.Update();
                CommunicationController.networkSession = networkSession;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// Updates the login screen
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        private void LoginUpdate(GameTime gameTime)
        {
            //Will switch to MainMenu upon succesful log in.

            //Checks keys typed and stores them to text.
            foreach (Keys key in keysToCheck)
            {
                if (CheckKey(key))
                {
                    AddKeyToText(key, gameTime);
                    break;
                }
            }

            //Checks if username field is clicked, to set it's state to active
            if (IsClicked(userNameBox, lastMouseState, currentMouseState))
            {
                usernameActive = true;
                passwordActive = false;
                if (un.Length == 0)
                {
                    //makes sure to clear out text if pw was entered first.
                    //redundancy check on clearing out un. 
                    text = "";
                    un = "";
                }
                else
                    text = un; //Sets text to equal the current un, so that text and un synchronize
                //for text input recieved from user. 
            }
            //Checks if password field is clicked, to set it's state to active
            else if (IsClicked(passwordBox, lastMouseState, currentMouseState))
            {
                usernameActive = false;
                passwordActive = true;
                if (pw.Length == 0)
                {
                    //makes sure to clear out text if un was entered first.
                    //redundancy check on clearing out pw.
                    text = "";
                    pw = "";
                }
                else
                    text = pw; //Sets text to equal the current pw, so that text and pw synchronize
                //for text input recieved from user. 
            }

            //Synchronizes un and text every update. 
            if (usernameActive)
            {
                if (text.Contains("|"))
                {
                    text = text.Substring(0, text.Length - 1);
                    un = text;
                    if (DatabaseController.LogIn(un, pw))
                    {
                        loginAttempt = 0;
                        gameState = GameState.MainMenu;
                        ClearLogin();
                    }
                    else
                    {
                        loginAttempt++;

                    }
                }
                else if (text.Contains(">"))
                {
                    text = text.Substring(0, text.Length - 1);
                    un = text;
                    usernameActive = false;
                    passwordActive = true;
                    text = pw;
                }
                else
                    un = text;
            }
            else if (passwordActive)
            {
                //synchronizes pw and text every update
                if (text.Contains("|"))
                {
                    text = text.Substring(0, text.Length - 1);
                    pw = text;
                    if (DatabaseController.LogIn(un, pw))
                    {
                        loginAttempt = 0;
                        gameState = GameState.MainMenu;
                        ClearLogin();
                    }
                    else
                    {
                        loginAttempt++;
                    }
                }
                else if (text.Contains(">"))
                {
                    text = text.Substring(0, text.Length - 1);
                    pw = text;
                    usernameActive = true;
                    passwordActive = false;
                    text = un;
                }
                else
                    pw = text;
                //creates a password mask so that no one can see what the
                //users password is. so instead of "abc" you see "***"
                while (pwMask.Length < pw.Length)
                {
                    pwMask = pwMask + '*';
                }
                //Removes the last * character as player deletes characters from password. 
                while (pwMask.Length > pw.Length)
                {
                    pwMask = pwMask.Remove(pw.Length);
                }
            }

            //Handler for Login
            if (loginMenu["Login"].IsClicked(lastMouseState, currentMouseState))
            {
                createResult = "";
                if (DatabaseController.LogIn(un, pw))
                {
                    loginAttempt = 0;
                    gameState = GameState.MainMenu;
                    ClearLogin();
                }
                else
                {
                    loginAttempt++;

                }
            }

            //Handler for Cancel. Clears out all text input by the user. Clears password Mask. 
            if (loginMenu["Cancel"].IsClicked(lastMouseState, currentMouseState))
            {
                createResult = "";
                ClearLogin();
            }

            //Handler for Creating an account. Uses the username and password fields to pass
            //to the Database the new Account Username and Password
            if (loginMenu["Create"].IsClicked(lastMouseState, currentMouseState))
            {
                loginAttempt = 0;
                createResult = DatabaseController.CreateAccount(un, pw);
            }

        }

        /// <summary>
        /// Checks if an area is clicked
        /// </summary>
        /// <param name="clickableArea">The area to check</param>
        /// <param name="last">The previous state of the mouse</param>
        /// <param name="current">The current state of the mouse</param>
        /// <returns></returns>
        public bool IsClicked(Rectangle clickableArea, MouseState last, MouseState current)
        {
            if (last.LeftButton == ButtonState.Pressed && current.LeftButton == ButtonState.Released)
            {
                if (clickableArea.Contains(current.X, current.Y))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clears the login boxes
        /// </summary>
        public void ClearLogin()
        {
            un = "";
            pw = "";
            pwMask = "";
            text = "";
        }

        /// <summary>
        /// Updates the find session screen
        /// </summary>
        private void FindSessionUpdate()
        {
            //Go back to the main menu
            if (findMenu["Back"].IsClicked(lastMouseState, currentMouseState))
                gameState = GameState.MainMenu;
            else if (findMenu["Refresh"].IsClicked(lastMouseState, currentMouseState))
                FindSession();
            for(int i = 1; i < 8; i++)
            {
                String gamenumber = "Gamecard " + i;
                if(findMenu[gamenumber].IsClicked(lastMouseState, currentMouseState))
                {
                    JoinSession(i-1);
                }
            }
        }

        /// <summary>
        /// Allows another player to join an existing session
        /// </summary>
        /// <param name="sessionID">Id of the session</param>
        private void JoinSession(int sessionID)
        {
            //Join an existing NetworkSession
            try
            {
                networkSession = NetworkSession.Join(availableSessions[sessionID]);

                //Register for NetworkSessionEvents
                networkSession.GameStarted +=
                    new EventHandler<GameStartedEventArgs>(networkSession_GameStarted);
                networkSession.GameEnded +=
                    new EventHandler<GameEndedEventArgs>(networkSession_GameEnded);
                networkSession.GamerJoined +=
                    new EventHandler<GamerJoinedEventArgs>(networkSession_GamerJoined);
                networkSession.GamerLeft +=
                    new EventHandler<GamerLeftEventArgs>(networkSession_GamerLeft);
                networkSession.SessionEnded +=
                    new EventHandler<NetworkSessionEndedEventArgs>(networkSession_SessionEnded);

                //Set the correct GameState. The NetworkSession may have already started a game
                if (networkSession.SessionState == NetworkSessionState.Playing)
                    gameState = GameState.PlayingGame;
                else
                    gameState = GameState.GameLobby;

                CommunicationController.networkSession = networkSession;
            }
            catch(NullReferenceException nre)
            {
                gameMessages.Add(new DisplayMessage("The selected game is no longer available.",
                    TimeSpan.FromSeconds(2)));

                FindSession();
            }
            catch (NetworkSessionJoinException ex)
            {
                gameMessages.Add(new DisplayMessage("Failed to connect to session: " + ex.JoinError.ToString(),
                    TimeSpan.FromSeconds(2)));

                //Check for Sessions again
                FindSession();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gameTime"></param>
        private void PlayingGameUpdate(GameTime gameTime)
        {


            //Check to see if the player wants to quit
            if (ButtonPressed(Buttons.Back) || KeyPressed(Keys.Escape))
            {
                //If the player is the host then the game is exited
                //but the session stays alive
                if (networkSession.IsHost)
                {
                    networkSession.EndGame();
                    CommunicationController.networkSession = networkSession;
                }
                //Other players leave the session
                else
                {
                    networkSession.Dispose();
                    networkSession = null;
                    gameState = GameState.MainMenu;
                    CommunicationController.networkSession = networkSession;
                }
                return;
            }

            CheckForSelectedPlayingObjects();
            CheckUserToolsClick();

            foreach (Keys key in keysToCheck)
            {
                if (CheckKey(key))
                {
                    AddKeyToText(key, gameTime);
                    break;
                }
            }
            if (gameOverTracker == networkSession.AllGamers.Count)
            {
                gameState = GameState.GameOver;
            }

            buttons.UpdateButtons(doorDeck, treasureDeck, focusCard, lastMouseState, currentMouseState, doorDiscard, treasureDiscard);
            buttons.SubmitButtonAction(lastMouseState, currentMouseState, doorDeck, treasureDeck, focusCard, player, playingField, doorDiscard, treasureDiscard);

            base.Update(gameTime);

            ReceiveNetworkData(gameTime);

            playingField.Update();

            foreach (Player p in players)
            {
                p.Update();
            }
        }

        private void GameOverUpdate(GameTime gameTime)
        {
            if(!gameOverStatsPulled)
            {
                goss = new GameOverStatSet();
                foreach (Player p in players)
                {
                    goss.AddStat(p, DatabaseController.GetPlayerStats(p.Gamertag));
                }
                gameOverStatsPulled = true;
            }
            Vector2 gameoverbutton = new Vector2(800, 100);
            gameOverButton.Position = gameoverbutton;
            gameOverButton.ClickableArea = new Rectangle((int)gameoverbutton.X, (int)gameoverbutton.Y, 180, 80);
            if (gameOverButton.IsClicked(lastMouseState, currentMouseState))
            {
                goss.SaveStats(player);
                gameState = GameState.MainMenu;
                this.Reinitialize();
            }
        }

        private void CheckUserToolsClick()
        {
            if (currentMouseState.LeftButton == ButtonState.Pressed && lastMouseState.LeftButton == ButtonState.Released)
            {
                Point mousePosition = new Point(currentMouseState.X, currentMouseState.Y);
                if (diceClickArea.Contains(mousePosition))
                {
                    //Checks for mouse clicks at dice area
                    currentDieRoll = random.Next(1, 7);

                    string combinedText = networkSession.LocalGamers[0].Gamertag + " has rolled a " + currentDieRoll;

                    sendStringToPacketWriter(combinedText);
                    chatOrganizer(combinedText);
                    diceButtonClicked = true;
                }
                if (levelUpClickArea.Contains(mousePosition)) //checks for mouse clicks at level up button
                {
                    if (currentLevel < 10)
                    {
                        currentLevel++;
                        player.Level = currentLevel;
                        CommunicationController.PlayerChangedLevel(player.Gamertag, currentLevel.ToString());
                        string combinedText = networkSession.LocalGamers[0].Gamertag + " has leveled up to " + currentLevel;
                        sendStringToPacketWriter(combinedText);
                        chatOrganizer(combinedText);
                    }
                    levelUpClicked = true;
                }
                if (levelDownClickArea.Contains(mousePosition)) //checks for mouse clicks at level down button
                {
                    if (currentLevel > 1)
                    {
                        currentLevel--;
                        player.Level = currentLevel;
                        CommunicationController.PlayerChangedLevel(player.Gamertag, currentLevel.ToString());
                        string combinedText = networkSession.LocalGamers[0].Gamertag + " has leveled down to " + currentLevel;
                        sendStringToPacketWriter(combinedText);
                        chatOrganizer(combinedText);
                    }
                    levelDownClicked = true;
                }
                if (resetLifeClickArea.Contains(mousePosition)) //checks for mouse clicks at reset button
                {
                    if (currentLevel != 1)
                    {
                        currentLevel = 1;
                        string combinedText = networkSession.LocalGamers[0].Gamertag + " has reset their level";
                        sendStringToPacketWriter(combinedText);
                        chatOrganizer(combinedText);
                    }
                    resetButtonClicked = true;
                }
                if (gameOverClickArea.Contains(mousePosition))
                {
                    string combinedText;
                    gameOver = !gameOver;
                    int tempInt = 0;

                    if (gameOver == true)
                        tempInt = 1;
                    else
                        tempInt = -1;
                    gameOverTracker += tempInt;
                    if (gameOver == true)
                        combinedText = networkSession.LocalGamers[0].Gamertag + " has declared Game Over!";
                    else
                        combinedText = networkSession.LocalGamers[0].Gamertag + " has declared the game NOT over!";

                    CommunicationController.GameOverStatusChanged(combinedText, tempInt);

                    chatOrganizer(combinedText);
                }
            }

            if (currentMouseState.LeftButton == ButtonState.Released && lastMouseState.LeftButton == ButtonState.Pressed)
            {
                levelUpClicked = false;
                levelDownClicked = false;
                resetButtonClicked = false;
                diceButtonClicked = false;
            }
        }

        private void CheckForSelectedPlayingObjects()
        {
            if (doorDeck.IsClicked(lastMouseState, currentMouseState))
            {
                doorDeck.Selected = true;
                treasureDeck.Selected = false;
                doorDiscard.Selected = false;
                treasureDiscard.Selected = false;
                focusCard = null;
            }
            else if (treasureDeck.IsClicked(lastMouseState, currentMouseState))
            {
                treasureDeck.Selected = true;
                treasureDiscard.Selected = false;
                doorDiscard.Selected = false;
                doorDeck.Selected = false;
                focusCard = null;
            }
            else if (doorDiscard.IsClicked(lastMouseState, currentMouseState) && doorDiscard.Cards.Count > 0)
            {
                doorDiscard.Selected = true;
                doorDeck.Selected = false;
                treasureDeck.Selected = false;
                treasureDiscard.Selected = false;
                selectedDeck = null;
            }
            else if (treasureDiscard.IsClicked(lastMouseState, currentMouseState) && treasureDeck.Cards.Count > 0)
            {
                treasureDiscard.Selected = true;
                doorDiscard.Selected = false;
                treasureDeck.Selected = false;
                doorDeck.Selected = false;
                selectedDeck = null;
            }
            else
            {
                foreach (Player p in players)
                {
                    Card c = p.ClickedACard(lastMouseState, currentMouseState);
                    if (c != null)
                    {
                        focusCard = c;
                        doorDiscard.Selected = false;
                        doorDeck.Selected = false;
                        treasureDeck.Selected = false;
                        treasureDiscard.Selected = false;
                        c.Selected = true;
                    }
                }
            }
            Card aCard = playingField.ClickedACard(lastMouseState, currentMouseState);
            if (aCard != null)
            {
                focusCard = aCard;
                doorDiscard.Selected = false;
                doorDeck.Selected = false;
                treasureDeck.Selected = false;
                treasureDiscard.Selected = false;
                aCard.Selected = true;
            }
        }

        //Update method for the GameLobby GameState
        private void GameLobbyUpdate(GameTime gameTime)
        {
            int number = networkSession.AllGamers.IndexOf(networkSession.LocalGamers[0]) + 1;

            String player_number = "Playercard " + number;

            //Move back to the main menu
            if (lobbyMenu["Host Back"].IsClicked(lastMouseState, currentMouseState) || lobbyMenu["Guest Back"].IsClicked(lastMouseState, currentMouseState))
            {
                networkSession.Dispose();
                gameState = GameState.MainMenu;
            }
            //Set the ready state for the player
            else if (lobbyMenu[player_number].IsClicked(lastMouseState, currentMouseState))
                networkSession.LocalGamers[0].IsReady = !networkSession.LocalGamers[0].IsReady;
            //Only the host can start the game
            else if (lobbyMenu["Can Start"].IsClicked(lastMouseState, currentMouseState))
                networkSession.StartGame();

            CommunicationController.networkSession = networkSession;

            foreach (Keys key in keysToCheck)
            {
                if (CheckKey(key))
                {
                    AddKeyToText(key, gameTime);
                    break;
                }
            }

            ReceiveNetworkData(gameTime);
        }

        //Update method for the create session method
        private void CreateSessionUpdate()
        {
            if (sessionMenu["Create a Match"].IsClicked(lastMouseState, currentMouseState))
                CreateSession(GameType.Match);
            else if (sessionMenu["Back"].IsClicked(lastMouseState, currentMouseState))
                gameState = GameState.MainMenu;
        }

        //Update method for the MainMenu GameState
        private void MainMenuUpdate()
        {
            if (usernameActive || passwordActive == true)
            {
                usernameActive = false;
                passwordActive = false;
            }
            if (mainMenu["Create a Session"].IsClicked(lastMouseState, currentMouseState))
                gameState = GameState.CreateSession;
            else if (mainMenu["Find a Session"].IsClicked(lastMouseState, currentMouseState))
                FindSession();
            else if (mainMenu["Exit"].IsClicked(lastMouseState, currentMouseState))
                Exit();
        }
        #endregion UpdateMethods

        #region ButtonCheckAndChatMethods
        private bool CheckKey(Keys theKey)
        {
            return (lastKeyState.IsKeyDown(theKey) && currentKeyState.IsKeyUp(theKey));
        }
        
        //checks what keys are pushed on the keyboard and either adds it to the current text, removes a value, or sends it to the chatbox
        private void AddKeyToText(Keys key, GameTime gameTime)
        {
            string newChar = "";

            switch (key)
            {
                case Keys.A: newChar += "a"; break;
                case Keys.B: newChar += "b"; break;
                case Keys.C: newChar += "c"; break;
                case Keys.D: newChar += "d"; break;
                case Keys.E: newChar += "e"; break;
                case Keys.F: newChar += "f"; break;
                case Keys.G: newChar += "g"; break;
                case Keys.H: newChar += "h"; break;
                case Keys.I: newChar += "i"; break;
                case Keys.J: newChar += "j"; break;
                case Keys.K: newChar += "k"; break;
                case Keys.L: newChar += "l"; break;
                case Keys.M: newChar += "m"; break;
                case Keys.N: newChar += "n"; break;
                case Keys.O: newChar += "o"; break;
                case Keys.P: newChar += "p"; break;
                case Keys.Q: newChar += "q"; break;
                case Keys.R: newChar += "r"; break;
                case Keys.S: newChar += "s"; break;
                case Keys.T: newChar += "t"; break;
                case Keys.U: newChar += "u"; break;
                case Keys.V: newChar += "v"; break;
                case Keys.W: newChar += "w"; break;
                case Keys.X: newChar += "x"; break;
                case Keys.Y: newChar += "y"; break;
                case Keys.Z: newChar += "z"; break;
                case Keys.D0: newChar += "0"; break;
                case Keys.D1: newChar += "1"; break;
                case Keys.D2: newChar += "2"; break;
                case Keys.D3: newChar += "3"; break;
                case Keys.D4: newChar += "4"; break;
                case Keys.D5: newChar += "5"; break;
                case Keys.D6: newChar += "6"; break;
                case Keys.D7: newChar += "7"; break;
                case Keys.D8: newChar += "8"; break;
                case Keys.D9: newChar += "9"; break;
                case Keys.Add: newChar += "+"; break;
                case Keys.OemPlus: newChar += "+"; break;
                case Keys.OemMinus: newChar += "-"; break;
                case Keys.Subtract: newChar += "-"; break;
                case Keys.Multiply: newChar += "*"; break;
                case Keys.Divide: newChar += "/"; break;
                case Keys.OemSemicolon: newChar += ";"; break;
                case Keys.OemBackslash: newChar += "\\"; break;
                case Keys.OemComma: newChar += ","; break;
                case Keys.OemQuestion: newChar += "?"; break;
                case Keys.Decimal: newChar += "."; break;
                case Keys.OemQuotes: newChar += "\""; break;
                case Keys.OemPeriod: newChar += "."; break;
                case Keys.Space: newChar += " "; break;
                case Keys.OemOpenBrackets: newChar += "["; break;
                case Keys.OemCloseBrackets: newChar += "]"; break;
                case Keys.Tab: newChar += ">"; break;
                case Keys.Back:
                    if (text.Length != 0)
                    {
                        text = text.Remove(text.Length - 1);

                    }
                    return;
                case Keys.Enter:
                    if (gameState.Equals(GameState.Login))
                    {
                        newChar += "|"; break;
                    }
                    else
                    {
                        if (text.Length != 0)
                        {
                            string combinedText = networkSession.LocalGamers[0].Gamertag + ": " + text;

                            sendStringToPacketWriter(combinedText);
                            chatOrganizer(combinedText);
                            text = text.Remove(0);
                        }
                        return;
                    }
            }
            if (currentKeyState.IsKeyDown(Keys.RightShift) ||
                currentKeyState.IsKeyDown(Keys.LeftShift))
            {
                newChar = newChar.ToUpper();
            }
            text += newChar;
        }

        private void chatOrganizer(String commandID)
        {
            if (commandID.Length > 34)
            {
                int lineNum = (commandID.Length / 34);
                string tempText = "";

                for (int current = 0; current <= lineNum; current++)
                {
                    tempText = "";
                    if (current != lineNum)
                    {
                        tempText = commandID.Substring(0 + (34 * current), 34);
                        chatBoxText.Add(tempText);
                    }
                    else
                    {
                        int mod = commandID.Length % 34;
                        tempText = commandID.Substring(0 + (34 * current), mod);
                        chatBoxText.Add(tempText);
                    }
                }
            }
            else
                chatBoxText.Add(commandID);
        }

        bool ButtonPressed(Buttons button)
        {
            //Dont process buttons when the guide is visible
            if (Guide.IsVisible)
                return false;
            return currentGamePadState.IsButtonDown(button) && lastGamePadState.IsButtonUp(button);
        }

        bool KeyPressed(Keys key)
        {
            return currentKeyState.IsKeyDown(key) && lastKeyState.IsKeyUp(key);
        }
        #endregion ButtonCheckAndChatMethods

        #region SendingRecievingDataMethods
        //takes a string and sends it to the packetwriter
        private void sendStringToPacketWriter(string combinedText)
        {
            foreach (LocalNetworkGamer gamer in networkSession.LocalGamers)
            {
                CommunicationController.packetWriter.Write(combinedText);
                gamer.SendData(CommunicationController.packetWriter, SendDataOptions.None);
            }
        }

        //Network Data reciever method.
        private void ReceiveNetworkData(GameTime gameTime)
        {
            LocalNetworkGamer gamer = networkSession.LocalGamers[0];

            while (gamer.IsDataAvailable)
            {
                NetworkGamer sender;
                gamer.ReceiveData(CommunicationController.packetReader, out sender);

                //Ignore if sender is local player
                if (!sender.IsLocal)
                {
                    String commandID = CommunicationController.packetReader.ReadString();
                    if (commandID.Equals("!"))
                    {
                        BoardUpdater.UpdateBoardAfterPlayerAction(CommunicationController.packetReader.ReadString(), doorDeck, treasureDeck, masterDeck, doorDiscard, treasureDiscard, players, player, playingField);
                    }
                    else if (commandID.Equals("@"))
                    {
                        List<Deck> decks = BoardUpdater.UpdateLocalDecks(masterDeck, graphics.GraphicsDevice, CommunicationController.packetReader);
                        doorDeck = decks[0];
                        treasureDeck = decks[1];
                    }
                    else if(commandID.Equals("^"))
                    {
                        String playerUpdated = BoardUpdater.UpdateLevels(players);
                        foreach (Player p in players)
                        {
                            if(p.Gamertag.Equals(playerUpdated))
                            {
                                chatOrganizer(playerUpdated + " changed their level to " + p.Level);
                            }
                        }
                    }
                    else if (commandID.Equals("~"))
                    {
                        chatOrganizer(CommunicationController.packetReader.ReadString());
                        gameOverTracker += CommunicationController.packetReader.ReadInt32();
                        
                    }
                    else
                    {
                        chatOrganizer(commandID);
                    }
                }

                base.Update(gameTime);
            }
        }
        #endregion SendingRecievingDataMethods

        #region SessionMethods
        //Create a new NetworkSession
        private void CreateSession(GameType gameType)
        {
            try
            {
                //if we have an existing network session, we need to dispose of it
                if (networkSession != null && !networkSession.IsDisposed)
                    networkSession.Dispose();

                //Create the NetworkSessionProperties to use for the session
                //Other players will use these to search for a session.
                NetworkSessionProperties sessionProperties = new NetworkSessionProperties();
                sessionProperties[(int)SessionProperties.GameType] = (int)gameType;
                sessionProperties[(int)SessionProperties.OtherCustomProperty] = 42;

                //Create the NetworkSession NetworkSessionType of SystemLink
                networkSession = NetworkSession.Create(NetworkSessionType.SystemLink, 1, 4, 0, sessionProperties);
                networkSession.AllowJoinInProgress = false;

                //Register for NetworkSession events
                networkSession.GameStarted +=
                    new EventHandler<GameStartedEventArgs>(networkSession_GameStarted);
                networkSession.GameEnded +=
                    new EventHandler<GameEndedEventArgs>(networkSession_GameEnded);
                networkSession.GamerJoined +=
                    new EventHandler<GamerJoinedEventArgs>(networkSession_GamerJoined);
                networkSession.GamerLeft +=
                    new EventHandler<GamerLeftEventArgs>(networkSession_GamerLeft);
                networkSession.SessionEnded +=
                    new EventHandler<NetworkSessionEndedEventArgs>(networkSession_SessionEnded);
                CommunicationController.networkSession = networkSession;
                //Move the game into the GameLobby state
                gameState = GameState.GameLobby;
            }
            catch (GamerPrivilegeException e)
            {
                System.Console.WriteLine(e.Message);
                string error = String.Format("A signed in gamer profile is required to perform this operation. \n"
                    + "There are no profiles currently signed in");
                gameMessages.Add(new DisplayMessage(error, TimeSpan.FromSeconds(2)));
            }
        }

        //Event handler for the NetworkSession.gameStarted event
        //This event is fired when the host call NetworkSession.StartGame
        void networkSession_GameStarted(object sender, GameStartedEventArgs e)
        {
            gameMessages.Add(new DisplayMessage("Game Started", TimeSpan.FromSeconds(2)));
            //Move the game into the PlayingGame state
            gameState = GameState.PlayingGame;
        }

        //Event handler for the NetworkSession.GameEnded event
        //This event is fired when the host call NetworkSession.EndGame
        void networkSession_GameEnded(object sender, GameEndedEventArgs e)
        {
            gameMessages.Add(new DisplayMessage("Game Ended", TimeSpan.FromSeconds(2)));
            //Move the game into the GameLobby state
            gameState = GameState.GameLobby;
        }

        //Event handler for the NetworkSession.GamerJoined event
        //This event is fired when someone joins the session
        //This event will fire even for local gamers
        void networkSession_GamerJoined(object sender, GamerJoinedEventArgs e)
        {
            gameMessages.Add(new DisplayMessage("Gamer joined: " + e.Gamer.Gamertag, TimeSpan.FromSeconds(2)));
            //Add a new GameObject that we will use to store game state for the player
            //If the local gamer, it will display the player at the bottom of the screen
            if (networkSession.LocalGamers[0] == e.Gamer)
            {
                player.Initialize(/*Content.Load<Texture2D>("player_test_card"),*/ new Vector2(300, 745), "bottom");
                player.Gamertag = e.Gamer.Gamertag;
                player.Level = 1;
                players.Add(player);

            }
            //If the 2nd player, it will display across the table from local player
            else if (networkSession.RemoteGamers[0] == e.Gamer)
            {
                e.Gamer.Tag = new GameObject(new Vector2(640 - (e.Gamer.Gamertag.Length * 15), 50));
                player2 = new Player();
                player2.Level = 1;
                remotePlayers = new List<Player>();
                remotePlayers.Add(player2);
                player2.Initialize(new Vector2(590, 50), "top");
                player2.Gamertag = e.Gamer.Gamertag;
                players.Add(player2);
                if (networkSession.LocalGamers[0].IsHost)
                {
                    CommunicationController.UpdateRemoteDecks(doorDeck, treasureDeck);
                }
            }
            //If player 3, it will display on the left side of the table.
            else if (networkSession.RemoteGamers[1] == e.Gamer)
            {
                e.Gamer.Tag = new GameObject(new Vector2(20, 330));
                player3 = new Player();
                player3.Level = 1;
                remotePlayers.Add(player3);
                player3.Initialize(new Vector2(5, 285), "left");
                player3.Gamertag = e.Gamer.Gamertag;
                players.Add(player3);
                if (networkSession.LocalGamers[0].IsHost)
                {
                    CommunicationController.UpdateRemoteDecks(doorDeck, treasureDeck);
                }
            }
            //If player 4 it will display on the right side of the table. 
            else if (networkSession.RemoteGamers[2] == e.Gamer)
            {
                e.Gamer.Tag = new GameObject(new Vector2(1220 - (e.Gamer.Gamertag.Length * 15), 330));
                player4 = new Player();
                player4.Level = 1;
                remotePlayers.Add(player4);
                player4.Initialize(new Vector2(1000, 285), "right");
                player4.Gamertag = e.Gamer.Gamertag;
                players.Add(player4);
                if (networkSession.LocalGamers[0].IsHost)
                {
                    CommunicationController.UpdateRemoteDecks(doorDeck, treasureDeck);
                }
            }
        }

        //Event handler for the NetworkSession.GamerLeft event
        //This event is fired when a player leaves the session
        void networkSession_GamerLeft(object sender, GamerLeftEventArgs e)
        {
            gameMessages.Add(new DisplayMessage("Gamer Left: " + e.Gamer.Gamertag, TimeSpan.FromSeconds(2)));
            List<Player> remaining = new List<Player>();
            foreach (Player aplayer in players)
            {
                if (player.Gamertag.Equals(e.Gamer.Gamertag))
                {
                    aplayer.Left(doorDiscard, treasureDiscard);
                }
                else
                {
                    remaining.Add(aplayer);
                }
            }
            players.Clear();
            foreach(Player theplayer in remaining)
            {
                players.Add(theplayer);
            }
        }

        //Event handler for the NetworkSession.SessionEnded event
        //This event is fired when your connection to the NetworkSession is ended
        void networkSession_SessionEnded(object sender, NetworkSessionEndedEventArgs e)
        {
            gameMessages.Add(new DisplayMessage("Session Ended: " + e.EndReason.ToString(), TimeSpan.FromSeconds(2)));
            //Since we have disconnected we clean up the NetworkSession
            if (networkSession != null && !networkSession.IsDisposed)
                networkSession.Dispose();

            //Move the game into the MainMenu state
            gameState = GameState.MainMenu;
            this.Reinitialize();
        }



        //Method to start the search for a NetworkSession
        private void FindSession()
        {
            //Dispose of any previous session
            if (networkSession != null && !networkSession.IsDisposed)
                networkSession.Dispose();

            //Define the type of session we want to search for using the 
            //NetworkSessionProperties.
            //We only set the OtherCustomProperty
            NetworkSessionProperties sessionProperties = new NetworkSessionProperties();
            sessionProperties[(int)SessionProperties.OtherCustomProperty] = 42;

            //Find an available NetworkSession
            availableSessions = NetworkSession.Find(NetworkSessionType.SystemLink, 1, sessionProperties);

            //Move the game into the FindSession state
            gameState = GameState.FindSession;
            CommunicationController.networkSession = networkSession;

        }
        #endregion SessionMethods

        #region DrawMethods
        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            //Switch to call the correct draw method for the
            //current game state
            switch (gameState)
            {
                case GameState.Login:
                    LoginDraw();
                    break;

                case GameState.MainMenu:
                    MainMenuDraw();
                    break;

                case GameState.CreateSession:
                    CreateSessionDraw();
                    break;

                case GameState.GameLobby:
                    GameLobbyDraw();
                    break;

                case GameState.PlayingGame:
                    PlayingGameDraw();
                    break;

                case GameState.FindSession:
                    FindSessionDraw();
                    break;

                case GameState.GameOver:
                    GameOverDraw();
                    break;
            }

            //Draw the current display message
            if (gameMessages.Count > 0)
            {
                DisplayMessage currentMessage = gameMessages[0];

                spriteBatch.Begin();
                Vector2 stringSize = spriteFont.MeasureString(gameMessages[0].Message);
                spriteBatch.DrawString(spriteFont, gameMessages[0].Message, new Vector2((1280 - stringSize.X) / 2.0f, 500), Color.White);
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        //Draw method for the Login GameState
        private void LoginDraw()
        {
            spriteBatch.Begin();
            //if(usernameActive)
            //    spriteBatch.Draw(chatRectangle, userNameBoxOutline, Color.Yellow);
            bg.Draw(spriteBatch);
            loginMenu.Draw(spriteBatch);

            spriteBatch.Draw(chatRectangle, userNameBox, Color.White);
            spriteBatch.Draw(chatRectangle, passwordBox, Color.White);

            //string tempText = this.text;
            //if (this.text.Length > 34)
            //{
            //    tempText = this.text.Substring(this.text.Length - 34, 34);

            //}
            spriteBatch.DrawString(textFont, "Username: ", new Vector2(userNameBox.X - 65, userNameBox.Y), Color.White);
            spriteBatch.DrawString(textFont, "Password: ", new Vector2(passwordBox.X - 65, passwordBox.Y), Color.White);
            if (usernameActive)
            {
                spriteBatch.DrawString(textFont, un + "|", new Vector2(userNameBox.X, userNameBox.Y), Color.Black);
                spriteBatch.DrawString(textFont, pwMask, new Vector2(passwordBox.X, passwordBox.Y), Color.Black);
            }
            else
            {
                spriteBatch.DrawString(textFont, un, new Vector2(userNameBox.X, userNameBox.Y), Color.Black);
                spriteBatch.DrawString(textFont, pwMask + "|", new Vector2(passwordBox.X, passwordBox.Y), Color.Black);
            }

            if (loginAttempt > 0)
                spriteBatch.DrawString(textFont, "Login failed. Incorrect Username And/Or Password", new Vector2(passwordBox.X, passwordBox.Y + 30), Color.Red);

            spriteBatch.DrawString(textFont, createResult, new Vector2(passwordBox.X, passwordBox.Y + 30), Color.Red);
            
            if (loginMenu["Login"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)loginMenu["Login"].StartPosition.X,
                            (int)loginMenu["Login"].StartPosition.Y,
                            loginMenu["Login"].clickableArea.Width,
                            loginMenu["Login"].clickableArea.Height);
            }
            else if(loginMenu["Cancel"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)loginMenu["Cancel"].StartPosition.X,
                            (int)loginMenu["Cancel"].StartPosition.Y,
                            loginMenu["Cancel"].clickableArea.Width,
                            loginMenu["Cancel"].clickableArea.Height);
            }
            else if (loginMenu["Create"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)loginMenu["Create"].StartPosition.X,
                            (int)loginMenu["Create"].StartPosition.Y,
                            loginMenu["Create"].clickableArea.Width,
                            loginMenu["Create"].clickableArea.Height);
            }

            spriteBatch.End();
        }

        //Draw method for the FindSession GameState
        private void FindSessionDraw()
        {
            spriteBatch.Begin();
            bg.Draw(spriteBatch);
            findMenu.Draw(spriteBatch);

            int gamenumber = 0;
            
            //Write message if there are no sessions found
            if (availableSessions.Count == 0)
            {
                findMenu["No Sessions"].IsActive = true;
                for(int i = 1; i < 8; i++)
                {
                    String temp = "Gamecard " + i;
                    findMenu[temp].IsActive = false;
                }
            }
            else
            {
                findMenu["No Sessions"].IsActive = false;
                //Print out a list of the available sessions
                foreach (AvailableNetworkSession session in availableSessions)
                {
                    gamenumber++;

                    if (gamenumber <= 8)
                    {
                        String gameCard = "Gamecard " + (gamenumber);

                        findMenu[gameCard].IsActive = true;

                        int totalSlots = session.CurrentGamerCount + session.OpenPublicGamerSlots;

                        spriteBatch.DrawString(spriteFont, "Join " + session.HostGamertag + "'s Game",
                                               new Vector2(findMenu[gameCard].StartPosition.X + 25,
                                                           findMenu[gameCard].StartPosition.Y), Color.Black);
                        spriteBatch.DrawString(spriteFont, session.CurrentGamerCount + "/" + totalSlots,
                                               new Vector2(findMenu[gameCard].StartPosition.X + 975,
                                                           findMenu[gameCard].StartPosition.Y), Color.Black);
                    }
                }

                for(int i = gamenumber + 1; i < 8; i++)
                {
                    string temp = "Gamecard " + i;
                    findMenu[temp].IsActive = false;
                }

                for (int i = 1; i < 8; i++)
                {
                    string temp = "Gamecard " + i;
                    if (findMenu[temp].IsHoverOver(currentMouseState))
                    {
                        DrawOutline(spriteBatch, (int)findMenu[temp].StartPosition.X,
                                    (int)findMenu[temp].StartPosition.Y,
                                    findMenu[temp].clickableArea.Width,
                                    findMenu[temp].clickableArea.Height);
                    }
                }
            }
            if (findMenu["Back"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)findMenu["Back"].StartPosition.X,
                            (int)findMenu["Back"].StartPosition.Y,
                            findMenu["Back"].clickableArea.Width,
                            findMenu["Back"].clickableArea.Height);
            }
            else if (findMenu["Refresh"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)findMenu["Refresh"].StartPosition.X,
                            (int)findMenu["Refresh"].StartPosition.Y,
                            findMenu["Refresh"].clickableArea.Width,
                            findMenu["Refresh"].clickableArea.Height);
            }
            spriteBatch.End();
        }

        private void PlayingGameDraw()
        {
            spriteBatch.Begin();
            bg.Draw(spriteBatch);
            DrawChatBox(spriteBatch, graphics.PreferredBackBufferWidth - 260, graphics.PreferredBackBufferHeight - 255, 260, 255);
            DrawDice(spriteBatch);
            DrawLevelBox(spriteBatch);
            DrawGameOverBox(spriteBatch);
            spriteBatch.Draw(doorDeck.CoverImage, new Vector2(doorDeck.ClickableArea.X, doorDeck.ClickableArea.Y), Color.White);
            spriteBatch.Draw(treasureDeck.CoverImage, new Vector2(treasureDeck.ClickableArea.X, doorDeck.ClickableArea.Y), Color.White);

            doorDiscard.Draw(spriteBatch);
            treasureDiscard.Draw(spriteBatch);

            buttons.DrawButtons(spriteBatch);

            foreach (Player p in players)
            {
                p.Draw(spriteBatch, textFont);
            }
            if (focusCard != null)
            {
                if (focusCard.Selected)
                {
                    focusCard.Position = new Vector2(1090, 625);
                    spriteBatch.Draw(focusCard.LargeFrontImage, focusCard.Position, Color.White);
                }
            }
            playingField.Draw(spriteBatch);
            spriteBatch.End();
        }

        //draws the level up/down box, buttons, reset level
        private void DrawLevelBox(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(levelBoxTexture, new Rectangle(1340, 649, 85, 58), Color.White);
            if (!levelUpClicked)
                spriteBatch.Draw(levelUpButtonTexture, levelUpClickArea, Color.White);
            else
                spriteBatch.Draw(levelUpButtonTexture, animateButton(levelUpClickArea), Color.White);
            if (!levelDownClicked)
                spriteBatch.Draw(levelDownButtonTexture, levelDownClickArea, Color.White);
            else
                spriteBatch.Draw(levelDownButtonTexture, animateButton(levelDownClickArea), Color.White);

            if (!resetButtonClicked)
                spriteBatch.Draw(resetButtonTexture, resetLifeClickArea, Color.White);
            else
                spriteBatch.Draw(resetButtonTexture, animateButton(resetLifeClickArea), Color.White);

            string tempString;
            if (currentLevel < 10)
                tempString = "0" + currentLevel.ToString();
            else
                tempString = currentLevel.ToString();
            spriteBatch.DrawString(levelFont, tempString, new Vector2(1350, 655), Color.Black);
        }

        private void DrawGameOverBox(SpriteBatch spriteBatch)
        {
            spriteBatch.DrawString(textFont, "Game Over?", new Vector2(1500, 15), Color.White);
            if (gameOver == false)
                spriteBatch.Draw(gameOverUnchecked, gameOverClickArea, Color.White);
            else
                spriteBatch.Draw(gameOverChecked, gameOverClickArea, Color.White);
        }

        //draws the dice
        private void DrawDice(SpriteBatch spriteBatch)
        {
            if (currentDieRoll == 1)
                if (!diceButtonClicked)
                    spriteBatch.Draw(die1Texture, diceClickArea, Color.White);
                else
                    spriteBatch.Draw(die1Texture, animateButton(diceClickArea), Color.White);
            else if (currentDieRoll == 2)
                if (!diceButtonClicked)
                    spriteBatch.Draw(die2Texture, diceClickArea, Color.White);
                else
                    spriteBatch.Draw(die2Texture, animateButton(diceClickArea), Color.White);
            else if (currentDieRoll == 3)
                if (!diceButtonClicked)
                    spriteBatch.Draw(die3Texture, diceClickArea, Color.White);
                else
                    spriteBatch.Draw(die3Texture, animateButton(diceClickArea), Color.White);
            else if (currentDieRoll == 4)
                if (!diceButtonClicked)
                    spriteBatch.Draw(die4Texture, diceClickArea, Color.White);
                else
                    spriteBatch.Draw(die4Texture, animateButton(diceClickArea), Color.White);
            else if (currentDieRoll == 5)
                if (!diceButtonClicked)
                    spriteBatch.Draw(die5Texture, diceClickArea, Color.White);
                else
                    spriteBatch.Draw(die5Texture, animateButton(diceClickArea), Color.White);
            else if (currentDieRoll == 6)
                if (!diceButtonClicked)
                    spriteBatch.Draw(die6Texture, diceClickArea, Color.White);
                else
                    spriteBatch.Draw(die6Texture, animateButton(diceClickArea), Color.White);
        }

        //draws the chatbox
        private void DrawChatBox(SpriteBatch spriteBatch, int start_x, int start_y, int width, int height)
        {
            spriteBatch.Draw(chatOutline, new Rectangle(start_x, start_y, width, height), Color.Black);
            spriteBatch.Draw(chatRectangle, new Rectangle(start_x + 5, start_y + 5, width - 10, height - 10), Color.White);
            spriteBatch.Draw(chatOutline, new Rectangle(start_x + 5, start_y + height - 25 , width - 10, 2), Color.Silver);

            string tempText = this.text;
            if (this.text.Length > 34)
            {
                tempText = this.text.Substring(this.text.Length - 34, 34);

            }
            spriteBatch.DrawString(textFont, tempText, new Vector2(start_x + 10, start_y + height - 20), Color.Black);

            if (chatBoxText.Count > 0)
            {
                int temp = 0;
                if (gameState == GameState.PlayingGame && chatBoxText.Count > 11)
                    chatBoxText.RemoveRange(0, chatBoxText.Count - 11);
                else if (gameState == GameState.GameLobby && chatBoxText.Count > 3)
                    chatBoxText.RemoveRange(0, chatBoxText.Count - 3);
                foreach (string chatText in chatBoxText)
                {
                    spriteBatch.DrawString(textFont, chatText, new Vector2(start_x + 5, start_y + 5 + (temp * 20)), Color.Black);
                    temp++;
                }
            }
        }

        //Helper method to animate a button by moving it by 1 pixel down and to the left
        private Rectangle animateButton(Rectangle rect)
        {
            return new Rectangle(rect.X - 1, rect.Y + 1, rect.Width, rect.Height);
        }

        private void GameLobbyDraw()
        {
            spriteBatch.Begin();
            bg.Draw(spriteBatch);
            

            Int32 playerCount = networkSession.AllGamers.Count;
            if (playerCount == 4)
            {
                lobbyMenu["Playercard 2"].IsActive = true;
                lobbyMenu["Playercard 3"].IsActive = true;
                lobbyMenu["Playercard 4"].IsActive = true;
            }
            else if (playerCount == 3)
            {
                lobbyMenu["Playercard 2"].IsActive = true;
                lobbyMenu["Playercard 3"].IsActive = true;
                lobbyMenu["Playercard 4"].IsActive = false;
                lobbyMenu["PlayerReady 4"].IsActive = false;
            }
            else if (playerCount == 2)
            {
                lobbyMenu["Playercard 2"].IsActive = true;
                lobbyMenu["Playercard 3"].IsActive = false;
                lobbyMenu["PlayerReady 3"].IsActive = false;
                lobbyMenu["Playercard 4"].IsActive = false;
                lobbyMenu["PlayerReady 4"].IsActive = false;
            }
            else if (playerCount == 1)
            {
                lobbyMenu["Playercard 2"].IsActive = false;
                lobbyMenu["PlayerReady 2"].IsActive = false;
                lobbyMenu["Playercard 3"].IsActive = false;
                lobbyMenu["PlayerReady 3"].IsActive = false;
                lobbyMenu["Playercard 4"].IsActive = false;
                lobbyMenu["PlayerReady 4"].IsActive = false;
            }

            lobbyMenu.Draw(spriteBatch);

            DrawChatBox(spriteBatch, (int)lobbyMenu.StartPosition.X + 49, (int)lobbyMenu.StartPosition.Y + 649, 525, 100);

            //Draw all games in the lobby
            float drawOffSet = 0;
            int playerNumber = 1;
            foreach (NetworkGamer networkGamer in networkSession.AllGamers)
            {
                spriteBatch.DrawString(spriteFont, networkGamer.Gamertag +
                    ((networkGamer.IsTalking) ? " - TALKING" : ""),
                    new Vector2(lobbyMenu.StartPosition.X + 75, lobbyMenu.StartPosition.Y + 85 + drawOffSet), Color.Black);
                drawOffSet += 150;

                String playerCheckbox = "PlayerReady " + playerNumber;
                lobbyMenu[playerCheckbox].IsActive = networkGamer.IsReady;

                playerNumber++;

            }

            if (networkSession.LocalGamers[0].IsHost)
            {
                lobbyMenu["Host Back"].IsActive = true;

                if (networkSession.IsEveryoneReady)
                {
                    lobbyMenu["Can Start"].IsActive = true;
                    lobbyMenu["Cant Start"].IsActive = false;
                }
                else
                {
                    lobbyMenu["Can Start"].IsActive = false;
                    lobbyMenu["Cant Start"].IsActive = true;
                }
            }
            else
            {
                lobbyMenu["Guest Back"].IsActive = true;
            }
            if (lobbyMenu["Guest Back"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)lobbyMenu["Guest Back"].StartPosition.X,
                            (int)lobbyMenu["Guest Back"].StartPosition.Y,
                            lobbyMenu["Guest Back"].clickableArea.Width,
                            lobbyMenu["Guest Back"].clickableArea.Height);
            }
            else if (lobbyMenu["Host Back"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int) lobbyMenu["Host Back"].StartPosition.X,
                            (int) lobbyMenu["Host Back"].StartPosition.Y,
                            lobbyMenu["Host Back"].clickableArea.Width,
                            lobbyMenu["Host Back"].clickableArea.Height);
            }
            else if (lobbyMenu["Can Start"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)lobbyMenu["Can Start"].StartPosition.X,
                            (int)lobbyMenu["Can Start"].StartPosition.Y,
                            lobbyMenu["Can Start"].clickableArea.Width,
                            lobbyMenu["Can Start"].clickableArea.Height);
            }
            spriteBatch.End();
        }

        private void CreateSessionDraw()
        {
            spriteBatch.Begin();
            bg.Draw(spriteBatch);
            sessionMenu.Draw(spriteBatch);
            if (sessionMenu["Create a Match"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)sessionMenu["Create a Match"].StartPosition.X,
                            (int)sessionMenu["Create a Match"].StartPosition.Y,
                            sessionMenu["Create a Match"].clickableArea.Width,
                            sessionMenu["Create a Match"].clickableArea.Height);
            }
            else if (sessionMenu["Back"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)sessionMenu["Back"].StartPosition.X,
                            (int)sessionMenu["Back"].StartPosition.Y,
                            sessionMenu["Back"].clickableArea.Width,
                            sessionMenu["Back"].clickableArea.Height);
            }
            spriteBatch.End();
        }

        private void MainMenuDraw()
        {
            spriteBatch.Begin();
            bg.Draw(spriteBatch);
            mainMenu.Draw(spriteBatch);
            if (mainMenu["Create a Session"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)mainMenu["Create a Session"].StartPosition.X,
                            (int)mainMenu["Create a Session"].StartPosition.Y,
                            mainMenu["Create a Session"].clickableArea.Width,
                            mainMenu["Create a Session"].clickableArea.Height);
            }else if (mainMenu["Find a Session"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)mainMenu["Find a Session"].StartPosition.X,
                            (int)mainMenu["Find a Session"].StartPosition.Y,
                            mainMenu["Find a Session"].clickableArea.Width,
                            mainMenu["Find a Session"].clickableArea.Height);
            }else if (mainMenu["Exit"].IsHoverOver(currentMouseState))
            {
                DrawOutline(spriteBatch, (int)mainMenu["Exit"].StartPosition.X,
                            (int)mainMenu["Exit"].StartPosition.Y,
                            mainMenu["Exit"].clickableArea.Width,
                            mainMenu["Exit"].clickableArea.Height);
            }
            spriteBatch.End();
        }

        private void DrawOutline(SpriteBatch spriteBatch, int x, int y, int width, int height)
        {
            Texture2D boxOutline = new Texture2D(GraphicsDevice, 1, 1);
            boxOutline.SetData(new Color[] { Color.White });

            spriteBatch.Draw(boxOutline, new Rectangle(x, y, width, 6), Color.Gray);
            spriteBatch.Draw(boxOutline, new Rectangle(x, y, 6, height), Color.Gray);
            spriteBatch.Draw(boxOutline, new Rectangle(x + width - 6, y, 6, height), Color.Gray);
            spriteBatch.Draw(boxOutline, new Rectangle(x, y + height - 6, width, 6), Color.Gray);
        }

        private void GameOverDraw()
        {
            spriteBatch.Begin();
            bg.Draw(spriteBatch);
            if(goss != null)
            {
                goss.Draw(spriteBatch, spriteFont);
            }
            foreach (Player p in players)
            {
                if(p.Level == 10)
                {
                    spriteBatch.DrawString(spriteFont, (p.Gamertag +" Wins!!!!!!!"), new Vector2(600, 10), Color.Red );
                }
            }
            spriteBatch.Draw(gameOverButton.Image, gameOverButton.Position, Color.White);
            spriteBatch.End();
        }

        #endregion DrawMethods

    }

}
