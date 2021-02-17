namespace AttentionAndRetag.Retag
{
	/// <summary>
	/// Basic Clonable interface
	/// </summary>
	/// <typeparam name="T"></typeparam>
    public interface IClonable<T>:IObjectClonable
	{
		T Clone();
	}
}
