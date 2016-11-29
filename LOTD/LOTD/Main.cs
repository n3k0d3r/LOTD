using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Storage;
using OmegaEngine;

namespace LOTD
{
    /*************************************
     * GameState is used to define the game's current point. Main represents the main menu, Game represents in-game play, etc
     * ***********************************/
    enum GameState { Main, LoadSave, RPGGame, Battle };

    public class Main : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;

        GameState gameState = GameState.Main;

        SoundEffect bgm;
        SoundEffectInstance bgmInst;

        Save save;  // Used for checking, loading, and saving player data, map changes, etc. Also, like the creative object name? I know. It's so unconventional. I mean, this one semester I had this Computing & Data Structures class at my community college and my professor would always knock points down from my programs for this. I mean, why does he care? The algorithms are efficient and condensed. It should really have satisfied him, and I should have gotten 100% on all my projects. It also didn't help that he had a really heavy Indian accent and was not fluent or literate in English, which made his tests even harder. It took the whole class time for me or my classmates to even understand what we were supposed to do for the programming problems. He wasn't even technically a teacher, nor did he even have a degree in Computer Science, or computer related studies. He was an ELECTRICAL ENGINEER that LCC hired on the spot because they prefer hiring people with "real life experience" than people who could ACTUALLY TEACH. Oh, am I rambling? Oops.

        // Player class contains all information about the controlled character (i.e. position, source frame, etc)
        Player player;

        // Map class contains all information about the overworld RPG map and partially handles movement
        Map map;

        KeyboardState keyPress;
        KeyboardState prevKeyPress;
        GamePadState gamePad;
        GamePadState prevGamePad;

        Rectangle logoTrans;    // Logo's current position and transition
        Rectangle result;       // Size and position after transition
        bool trans;             // Whether or not transition is occurring
        float transMult;        // Multiplier for logo resize
        int transStep;          // Step size for logo transition
        int transDelay;         // Delay (in milliseconds) between steps

        int selected;           // Keep track of what slot is selected
        int option;             // Keep track of what option is selected
        bool confirm;           // Whether or not confirmation of new game, load game, or cancel is occurring

        Vector2 screenBounds;
        Rectangle windowBounds;
        const int BIT_WIDTH = 32;               // Set to sprite size in pixels

        public Main()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = Path.GetFullPath("Content");
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        }

        protected override void Initialize()
        {
            graphics.IsFullScreen = true;
            graphics.ApplyChanges();
            Globals.bitWidth = BIT_WIDTH;
            screenBounds = new Vector2(GraphicsDevice.Viewport.Bounds.Width, GraphicsDevice.Viewport.Bounds.Height);
            windowBounds = new Rectangle((int)((screenBounds.X % BIT_WIDTH) / 2), (int)((screenBounds.Y % BIT_WIDTH) / 2), (int)screenBounds.X - (int)screenBounds.X % BIT_WIDTH, (int)screenBounds.Y - (int)screenBounds.Y % BIT_WIDTH);

            this.IsMouseVisible = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            bgm = Content.Load<SoundEffect>("Audio/bgm");
            bgmInst = bgm.CreateInstance();
            font = Content.Load<SpriteFont>("Fonts/font");

            logoTrans = new Rectangle(Convert.ToInt32(screenBounds.X / 2 - 480), Convert.ToInt32(screenBounds.Y / 2 - 270), 960, 540);
            result = new Rectangle(Convert.ToInt32(screenBounds.X / 2 - 240), Convert.ToInt32(screenBounds.Y / 2 - 135) - 350, 480, 270);
            trans = false;
            transMult = .99f;
            transStep = 3;
            transDelay = 10;

            selected = 0;
            option = 0;
            confirm = false;
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            keyPress = Keyboard.GetState();
            gamePad = GamePad.GetState(PlayerIndex.One);
            if (keyPress.IsKeyDown(Keys.Escape) || gamePad.Buttons.Back == ButtonState.Pressed) { try { save.DeleteTempMaps(); } catch { } Exit(); }   // Once menus are finished, comment out this and only use for halting

            if (Globals.isBattling)
            {
                //gameState = GameState.Battle;
            }
            else if (!Globals.isBattling && gameState == GameState.Battle)
            {
                //gameState = GameState.RPGGame;
            }

            if (Globals.flags["Prison_Finish"])
            {
                // Make credits. For now, takes player back to main menu
                Globals.flags["Prison_Finish"] = false;
                gameState = GameState.Main;
            }

            if (gameState == GameState.Main)
            {
                if (trans && Globals.timer.ElapsedMilliseconds >= transDelay)
                {
                    logoTrans.Y = logoTrans.Y - transStep;
                    logoTrans.Width = Convert.ToInt32(logoTrans.Width * transMult);
                    logoTrans.Height = Convert.ToInt32(logoTrans.Height * transMult);
                    if (logoTrans.Y <= result.Y && logoTrans.Width <= result.Width && logoTrans.Height <= result.Height)
                    {
                        logoTrans = result;
                        trans = false;
                        logoTrans = new Rectangle(Convert.ToInt32(screenBounds.X / 2 - 480), Convert.ToInt32(screenBounds.Y / 2 - 270), 960, 540);
                        gameState = GameState.LoadSave;
                    }
                    if (logoTrans.Y < result.Y)
                        logoTrans.Y = result.Y;
                    if (logoTrans.Width < result.Width)
                        logoTrans.Width = result.Width;
                    if (logoTrans.Height < result.Height)
                        logoTrans.Height = result.Height;

                    logoTrans.X = Convert.ToInt32(screenBounds.X / 2 - logoTrans.Width / 2);

                    Globals.timer.Restart();
                }
                else if (keyPress.IsKeyDown(Keys.E) || gamePad.Buttons.A == ButtonState.Pressed)
                {
                    trans = true;
                    Globals.timer.Restart();
                }
            }
            else if (gameState == GameState.LoadSave)
            {
                if ((keyPress.IsKeyDown(Keys.Q) && prevKeyPress.IsKeyUp(Keys.Q)) || (gamePad.Buttons.B == ButtonState.Pressed && prevGamePad.Buttons.B == ButtonState.Released))
                {
                    try
                    {
                        save.DeleteTempMaps();
                    }
                    catch { }
                    Exit();
                }
                else if ((keyPress.IsKeyDown(Keys.W) && prevKeyPress.IsKeyUp(Keys.W)) || (gamePad.DPad.Up == ButtonState.Pressed && prevGamePad.DPad.Up == ButtonState.Released))
                {
                    if (!confirm)
                    {
                        selected--;
                        if (selected < 0)
                            selected = 2;
                    }
                    else
                    {
                        option--;
                        if (option < 0)
                            option = 2;
                    }
                }
                else if ((keyPress.IsKeyDown(Keys.S) && prevKeyPress.IsKeyUp(Keys.S)) || (gamePad.DPad.Down == ButtonState.Pressed && prevGamePad.DPad.Down == ButtonState.Released))
                {
                    if (!confirm)
                    {
                        selected++;
                        if (selected > 2)
                            selected = 0;
                    }
                    else
                    {
                        option++;
                        if (option > 2)
                            option = 0;
                    }
                }
                else if ((keyPress.IsKeyDown(Keys.E) && prevKeyPress.IsKeyUp(Keys.E)) || (gamePad.Buttons.A == ButtonState.Pressed && prevGamePad.Buttons.A == ButtonState.Released))
                {
                    if (confirm)
                    {
                        switch (option)
                        {
                            case 0:
                                save = new Save(selected + 1);
                                if (save.saveExists())
                                {
                                    confirm = false;
                                    selected = 0;
                                    option = 0;

                                    save.openFile();

                                    map = new Map(Content, spriteBatch, screenBounds, windowBounds, "Assets/tiles");
                                    player = new Player("Joseph");
                                    Map.setMapBounds();

                                    bgmInst.IsLooped = true;
                                    bgmInst.Play();
                                    gameState = GameState.RPGGame;
                                }
                                break;
                            case 1:
                                confirm = false;
                                selected = 0;
                                option = 0;

                                save = new Save(selected + 1);
                                save.newFile();
                                save.openFile();

                                map = new Map(Content, spriteBatch, screenBounds, windowBounds, "Assets/tiles");
                                player = new Player("Joseph");
                                Map.setMapBounds();

                                bgmInst.IsLooped = true;
                                bgmInst.Play();
                                gameState = GameState.RPGGame;

                                break;
                            case 2:
                                option = 0;
                                confirm = false;
                                break;
                        }
                    }
                    else
                    {
                        confirm = true;
                    }
                }
            }
            else if (gameState == GameState.RPGGame)
            {
                if (keyPress.IsKeyDown(Keys.Q) && prevKeyPress.IsKeyUp(Keys.Q))
                    save.saveFile();
                // Update all methods
                player.Update();
            }

            prevKeyPress = keyPress;
            prevGamePad = gamePad;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

            if (gameState == GameState.Main)
            {
                spriteBatch.Draw(Content.Load<Texture2D>("Assets/pixel"), new Rectangle(0, 0, Convert.ToInt32(screenBounds.X), Convert.ToInt32(screenBounds.Y)), new Color(64, 64, 64));
                spriteBatch.Draw(Content.Load<Texture2D>("Assets/logo"), logoTrans, Color.White);
                if (!trans)
                    spriteBatch.DrawString(font, "Press E To Start", new Vector2(screenBounds.X / 2 - font.MeasureString("Press E To Start").X / 2, screenBounds.Y / 2 + 300), Color.Black);
            }
            else if (gameState == GameState.LoadSave)
            {
                Texture2D ui = Content.Load<Texture2D>("Assets/16-9/uisaves");
                spriteBatch.Draw(ui, new Rectangle(0, 0, Convert.ToInt32(screenBounds.X), Convert.ToInt32(screenBounds.Y)), Color.White);
                if (!confirm)
                {
                    spriteBatch.Draw(Content.Load<Texture2D>("Assets/pixel"), new Rectangle(Convert.ToInt32(screenBounds.X / ui.Width) * 14, Convert.ToInt32(screenBounds.Y / ui.Height * (9 + 5 * selected)), Convert.ToInt32(screenBounds.X / ui.Width * 20), Convert.ToInt32(screenBounds.Y / ui.Height)), Color.White);
                    spriteBatch.Draw(Content.Load<Texture2D>("Assets/pixel"), new Rectangle(Convert.ToInt32(screenBounds.X / ui.Width) * 14, Convert.ToInt32(screenBounds.Y / ui.Height * (14 + 5 * selected)), Convert.ToInt32(screenBounds.X / ui.Width * 20), Convert.ToInt32(screenBounds.Y / ui.Height)), Color.White);
                    spriteBatch.Draw(Content.Load<Texture2D>("Assets/pixel"), new Rectangle(Convert.ToInt32(screenBounds.X / ui.Width) * 14, Convert.ToInt32(screenBounds.Y / ui.Height * (9 + 5 * selected)), Convert.ToInt32(screenBounds.X / ui.Width), Convert.ToInt32(screenBounds.Y / ui.Height * 5)), Color.White);
                    spriteBatch.Draw(Content.Load<Texture2D>("Assets/pixel"), new Rectangle(Convert.ToInt32(screenBounds.X / ui.Width) * 33, Convert.ToInt32(screenBounds.Y / ui.Height * (9 + 5 * selected)), Convert.ToInt32(screenBounds.X / ui.Width), Convert.ToInt32(screenBounds.Y / ui.Height * 5)), Color.White);
                }
                else
                {
                    spriteBatch.DrawString(font, "Load Game", new Vector2(screenBounds.X / ui.Width * 29, Convert.ToInt32(screenBounds.Y / ui.Height * (10.5 + 5 * selected))), Color.Black);
                    spriteBatch.DrawString(font, "New Game", new Vector2(screenBounds.X / ui.Width * 29, Convert.ToInt32(screenBounds.Y / ui.Height * (11.5 + 5 * selected))), Color.Black);
                    spriteBatch.DrawString(font, "Cancel", new Vector2(screenBounds.X / ui.Width * 29, Convert.ToInt32(screenBounds.Y / ui.Height * (12.5 + 5 * selected))), Color.Black);
                    spriteBatch.Draw(Content.Load<Texture2D>("Assets/pixel"), new Rectangle(Convert.ToInt32(screenBounds.X / ui.Width * 28), Convert.ToInt32(screenBounds.Y / ui.Height * (10.3 + option + 5 * selected)), Convert.ToInt32(screenBounds.X / ui.Width), Convert.ToInt32(screenBounds.Y / ui.Height)), Color.Black);
                }
                spriteBatch.DrawString(font, "Slot 1", new Vector2(screenBounds.X / ui.Width * 16, Convert.ToInt32(screenBounds.Y / ui.Height * 11.5)), Color.Black, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, "Slot 2", new Vector2(screenBounds.X / ui.Width * 16, Convert.ToInt32(screenBounds.Y / ui.Height * 16.5)), Color.Black, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                spriteBatch.DrawString(font, "Slot 3", new Vector2(screenBounds.X / ui.Width * 16, Convert.ToInt32(screenBounds.Y / ui.Height * 21.5)), Color.Black, 0f, Vector2.Zero, 2f, SpriteEffects.None, 0f);
                spriteBatch.Draw(Content.Load<Texture2D>("Assets/logo"), result, Color.White);
            }
            else if (gameState == GameState.RPGGame)
            {
                // Draws map
                map.Draw();

                // Draws controlled player
                player.Draw();

                // Draws overlay map
                map.DrawOverlay();

                // Draws enemies
                map.DrawEnemies();

                // Draws any ongoing events
                player.DrawEvents();
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
