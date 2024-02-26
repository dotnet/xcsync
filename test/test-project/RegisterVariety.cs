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

public class NoRegisterButStillValid : NSObject {
}
