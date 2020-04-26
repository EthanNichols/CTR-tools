﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace ctrviewer
{
    public partial class FirstPersonCamera : Camera
    {
        #region Локальные свойства


        public void SetRotation(float x, float y)
        {
            leftRightRot = x;
            upDownRot = y;
        }

        private float leftRightRot = 0;       // Угол поворота по оси Y
        private float _upDownRot = 0;
        private float upDownRot           // Угол поворота по оси X
        {
            get { return _upDownRot; }
            set
            {
                if ((value < MathHelper.Pi / 2) && (value > -MathHelper.Pi / 2))
                {
                    _upDownRot = value;
                }
            }
        }
        private MouseState originalMouseState;
        #endregion

        #region Глобальные свойства
        public float rotationSpeed = 0.1f;     // Скорость угла поворота
        public float translationSpeed = 2500f;    // Скорость перемещения
        #endregion

        #region Конструктор
        public FirstPersonCamera(Game game)
            : base(game)
        {

        }
        #endregion

        Vector3 slowdown = new Vector3(0, 0, 0);

        #region Изменение объекта

        #region Цикл обновления
        public void Update(GameTime gameTime, bool usemouse, bool move, MouseState newms, MouseState oldms)
        {
            base.Update(gameTime);

            float amount = (float)gameTime.ElapsedGameTime.TotalMilliseconds / 1000.0f;

            if (usemouse)
            {
                #region Изменение направления цели камера при помощи мыши

                if (oldms != newms)
                {
                    float xDifference = newms.X - oldms.X;
                    float yDifference = newms.Y - oldms.Y;

                    leftRightRot -= xDifference * 0.0025f;//rotationSpeed * xDifference * amount;
                    //System.Diagnostics.Debug.WriteLine("leftRightRot=" + leftRightRot);

                    upDownRot -= yDifference * 0.0025f;// rotationSpeed * yDifference* amount;
                    //System.Diagnostics.Debug.WriteLine("upDownRot=" + upDownRot);
                    UpdateViewMatrix();
                }
            }
            #endregion

            leftRightRot -= GamePad.GetState(Game1.activeGamePad).ThumbSticks.Right.X / 20.0f;
            upDownRot += GamePad.GetState(Game1.activeGamePad).ThumbSticks.Right.Y / 20.0f;

            #region Изменение пространственного положения камеры при помощи клавиатуры
            Vector3 moveVector = new Vector3(0, 0, 0);

            if (move)
            {

                KeyboardState keyState = Keyboard.GetState();
                GamePadState padState = GamePad.GetState(Game1.activeGamePad);

                if (keyState.IsKeyDown(Keys.W) || padState.DPad.Up == ButtonState.Pressed)
                    moveVector += new Vector3(0, 0, -1);
                if (keyState.IsKeyDown(Keys.S) || padState.DPad.Down == ButtonState.Pressed)
                    moveVector += new Vector3(0, 0, 1);
                if (keyState.IsKeyDown(Keys.D) || padState.DPad.Right == ButtonState.Pressed)
                    moveVector += new Vector3(1, 0, 0);
                if (keyState.IsKeyDown(Keys.A) || padState.DPad.Left == ButtonState.Pressed)
                    moveVector += new Vector3(-1, 0, 0);

                if (keyState.IsKeyDown(Keys.Q))
                    moveVector += new Vector3(0, 1, 0);
                if (keyState.IsKeyDown(Keys.Z))
                    moveVector += new Vector3(0, -1, 0);

                moveVector *= (Keyboard.GetState().IsKeyDown(Keys.LeftShift) ? 0.6f : 0.3f);

                if (Math.Abs(moveVector.X) > Math.Abs(slowdown.X))
                    slowdown.X = moveVector.X;

                if (Math.Abs(moveVector.Y) > Math.Abs(slowdown.Y))
                    slowdown.Y = moveVector.Y;

                if (Math.Abs(moveVector.Z) > Math.Abs(slowdown.Z))
                    slowdown.Z = moveVector.Z;

                moveVector += slowdown;

                slowdown *= 0.75f;


                moveVector += new Vector3(padState.ThumbSticks.Left.X, 0, -padState.ThumbSticks.Left.Y);

                // if (keyState.IsKeyDown(Keys.LeftShift) || padState.Buttons.A == ButtonState.Pressed)
                //     moveVector *= 2;

                moveVector *= (1 + padState.Triggers.Right * 3);


                if (keyState.IsKeyDown(Keys.Left))
                    leftRightRot += rotationSpeed;
                if (keyState.IsKeyDown(Keys.Right))
                    leftRightRot -= rotationSpeed;
                if (keyState.IsKeyDown(Keys.Up))
                    upDownRot += rotationSpeed;
                if (keyState.IsKeyDown(Keys.Down))
                    upDownRot -= rotationSpeed;
                #endregion


            }

            AddToCameraPosition(moveVector * amount);

        }
        #endregion

        public void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            AddToCameraPosition(new Vector3(0, 0, 0));
        }

        #region Изменение положения камеры и направления смотра
        private void AddToCameraPosition(Vector3 vectorToAdd)
        {
            Matrix cameraRotation = Matrix.CreateFromYawPitchRoll(leftRightRot, upDownRot, 0);
            Vector3 rotatedVector = Vector3.Transform(vectorToAdd, cameraRotation);
            Position += translationSpeed * (rotatedVector);
            Target += translationSpeed * (rotatedVector);
            UpdateViewMatrix();
        }
        #endregion

        #region Обновление матрицы вида
        private void UpdateViewMatrix()
        {
            Matrix cameraRotation = Matrix.CreateFromYawPitchRoll(leftRightRot, upDownRot, 0);

            Vector3 cameraOriginalTarget = new Vector3(0, 0, -1);
            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);

            Vector3 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            Vector3 cameraFinalTarget = Position + cameraRotatedTarget;

            Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            ViewMatrix = Matrix.CreateLookAt(Position, cameraFinalTarget, cameraRotatedUpVector);
        }
        #endregion
        #endregion


        public void Copy(GameTime gameTime, FirstPersonCamera c)
        {
            leftRightRot = c.leftRightRot;
            upDownRot = c.upDownRot;
            Position = c.Position;
            Target = c.Target;
            Update(gameTime);
        }
    }

}