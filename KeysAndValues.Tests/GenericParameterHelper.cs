namespace KeysAndValues.Tests;

public class GenericParameterHelper : IComparable<GenericParameterHelper>
{
    public GenericParameterHelper()
    {
        this.Data = new Random().Next();
    }

    public GenericParameterHelper(int data)
    {
        this.Data = data;
    }

    public int Data { get; set; }

    public int CompareTo(GenericParameterHelper? other)
    {
        if (other == null)
        {
            return 1;
        }
        if (this.Data < other.Data)
        {
            return -1;
        }
        else if (this.Data > other.Data)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is GenericParameterHelper other)
        {
            return this.Data == other.Data;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return this.Data;
    }
}
