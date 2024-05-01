namespace test_project;

[Register ("SkipRegistration", SkipRegistration = true)]
public class SkipRegistration : NSObject {
}

[Register ("NoSkip", SkipRegistration = false)]
public class NoSkip : NSObject {
}

[Register ("AlsoNoSkip")]
public class AlsoNoSkip : NSObject {
}

public class NoRegisterNotValid : ModelVariety {
}

[Protocol, Register]
public class ProtocolVariety : NSObject {
}


[Protocol, Register]
[Model]
public class ProtocolModelVariety : NSObject {
}

[Model]
[Register ("ObjectiveCModelVariety")]
public class ModelVariety : NSObject {
}

