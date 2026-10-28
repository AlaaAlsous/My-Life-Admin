namespace My_Life_Admin.Ai;

public class AiServiceException(string message, Exception? innerException = null)
    : Exception(message, innerException);
