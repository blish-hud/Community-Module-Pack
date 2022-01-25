namespace Universal_Search_Module.Services.SearchHandler {
    public struct WordScoreResult<T> {

        public T Result { get; set; }
        public int DiffScore { get; set; }

        public WordScoreResult(T result, int diffScore) {
            Result = result;
            DiffScore = diffScore;
        }
    }
}
