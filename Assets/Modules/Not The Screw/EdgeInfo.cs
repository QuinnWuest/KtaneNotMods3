namespace NotTheScrew
{
    internal class EdgeInfo
    {
        public int color;
        public int letter;

        public override string ToString()
        {
            return string.Format("color: {0}, letter: {1}", color, letter);
        }
    }
}