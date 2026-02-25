using System;
using Unity.Netcode;

[Serializable]
public struct IngredientData : INetworkSerializable
{
    public IngredientType Type;
    public IngredientState State;

    public IngredientData(IngredientType Type, IngredientState State)
    {
        this.Type = Type;
        this.State = State;
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is IngredientData other)
            return other.Type == Type && other.State == State;
        return false;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Type, State);
    }

    public override readonly string ToString()
    {
        return $"IngredientType: {Type} || IngredientState: {State}";
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref State);
    }
}