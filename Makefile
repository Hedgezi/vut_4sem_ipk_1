.PHONY: all clean

TARGET = ./ipk24chat-client
TARGET_DIR = ./ipk24chat
PROJECT_FILE = ./vut_ipk1.csproj

all: $(TARGET)

$(TARGET):
	dotnet publish $(PROJECT_FILE) -c Release -r linux-x64 -p:PublishSingleFile=true -p:DebugType=none --self-contained false -o $(TARGET_DIR) --nologo -v q
	mv $(TARGET_DIR)/vut_ipk1 $(TARGET)
	rm -rf $(TARGET_DIR)

clean:
	dotnet clean $(PROJECT_FILE)
	rm -rf $(TARGET)