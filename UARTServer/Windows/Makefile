
PROJECT_NAME    :=  FlintJVMUartServer
BUILD_DIR       :=  Build
BIN_DIR         :=  $(BUILD_DIR)/Bin
OBJECT_DIR      :=  $(BUILD_DIR)/Obj

CC              :=  g++

SOURCES       	:=  User/Src/main.cpp                                           \
                    User/Src/windows_tcp.cpp                                    \
                    User/Src/windows_uart.cpp

INCLUDES      	:=  User/Inc

OPT             :=  -g

CFLAGS          :=  -m32                                                        \
                    -std=gnu++11                                                \
                    -fdiagnostics-color=always                                  \
                    -static -static-libgcc -static-libstdc++                    \
                    -Wall                                                       \
                    -Wno-sign-compare                                           \
                    -Wno-strict-aliasing                                        \
                    -Wno-uninitialized                                          \
                    $(addprefix -I,$(INCLUDES))                                 \
                    $(OPT)

LIBS            :=  -lws2_32

OBJECTS         +=  $(addprefix $(OBJECT_DIR)/,$(SOURCES:.cpp=.o))

BUILD_DIRS      :=  $(sort $(dir $(OBJECTS))) $(BIN_DIR)
BUILD_DIRS      :=  $(BUILD_DIRS:/=)

GREEN           :=  \033[0;32m
CYAN            :=  \033[0;36m
RESET           :=  \033[0m

all: $(BIN_DIR)/$(PROJECT_NAME).exe

$(BUILD_DIRS):
	@mkdir -p $@

$(OBJECT_DIR)/%.o: %.cpp Makefile | $(BUILD_DIRS)
	@echo -e "$(CYAN)Compiling$(RESET)" $<
	@$(CC) -c $(CFLAGS) $< -o $@

$(BIN_DIR)/$(PROJECT_NAME).exe: $(OBJECTS)
	@$(CC) $(CFLAGS) $(OBJECTS) -o $@ $(LIBS)
	@echo -e "$(GREEN)Build complete$(RESET)"

clean:
	@rm -rf $(BUILD_DIR)
	@echo -e "$(GREEN)Clean complete$(RESET)"
