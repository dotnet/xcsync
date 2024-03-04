namespace test_project;


public class RegisterVariety {
}

[Register ("SkipRegistration", SkipRegistration = true)]
public class SkipRegistration : NSObject {
}

[Register ("NoSkip", SkipRegistration = false)]
public class NoSkip : NSObject {
}

[Register ("AlsoNoSkip")]
public class AlsoNoSkip : NSObject {
}

public class NoRegisterButStillValid : ModelVariety {
}

[Protocol]
public class ProtocolVariety : NSObject {
}

[Protocol]
[Model]
public class ProtocolModelVariety : NSObject {
}

[Model]
[Register ("ObjectiveCModelVariety")]
public class ModelVariety : NSObject {
}

