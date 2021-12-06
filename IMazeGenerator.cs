interface IMazeGenerator
{
    Maze Generate(int rows, int cols);
    void StartStepwiseGeneration(int rows, int cols);
    (bool finished, Maze maze) SingleStep();

    Cell CurrentCell { get; set; }
}