using Atma.Entities;

public interface ISystem
{
    void Init();
    void Update(float dt, EntityManager entityManager);
    void Draw(float dt, EntityManager entityManager);
}