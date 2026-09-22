public struct OptimizedTree
{
	public ushort positionX;  // Quantized to [0, 65535]
	public ushort positionY;
	public ushort positionZ;
	public byte widthScale;   // Quantized to [0, 255]
	public byte heightScale;
	public ushort rotation;   // Quantized to [0, 65535]
	public ushort color;
	public ushort lightmapColor;
	public byte prototypeIndex; // Limited to 256 prototypes
}