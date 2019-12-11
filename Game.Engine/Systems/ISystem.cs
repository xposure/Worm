using System;

public interface ISystem : IDisposable
{
    void Init();
    void Update(float dt);
    void Draw(float dt);

}