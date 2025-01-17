using Godot;
using System;
using NeonWarfare.Scripts.KludgeBox;
using NeonWarfare.Scripts.KludgeBox.Godot.Extensions;
using NeonWarfare.Scripts.Utils.Camera;

public partial class Background : TextureRect
{
    public override void _Process(double delta)
    {
        Camera2D camera = GetViewport().GetCamera2D();

        //Получаем размер области видимости
        Vector2 cameraSize = camera.GetViewportRect().Size / camera.Zoom;
        //Получаем позицию камеры в мире
        Vector2 cameraCenterPosition = camera.GlobalPosition;
        //Позиция верхнего левого угла камеры
        Vector2 cameraPosition = cameraCenterPosition - cameraSize / 2;
        //Получаем размер одного тайла текстуры
        Vector2 textureSize = Texture.GetSize();
        
        //Смещение текстуры на основе позиции камеры
        GlobalPosition = cameraPosition - cameraPosition.PosMod(textureSize);
        //Размер текстуры равен размеру экрана + запас с каждой стороны
        Size = cameraSize + textureSize * 2;
    }
}
