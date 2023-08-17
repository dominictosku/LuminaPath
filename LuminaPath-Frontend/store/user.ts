import { LoginUser, CreateUser } from "~/utils/request";
import { User, type Credentials } from "~/utils/user";

export const useUserStore = defineStore("user", () => {
  const storedStringValue: string | null = localStorage.getItem('loggedIn');
  const storedToken: string = localStorage.getItem('token') ?? ''
  const parsedBooleanValue: boolean = storedStringValue ? JSON.parse(storedStringValue) : false;
  const loggedIn: Ref<boolean> = ref(parsedBooleanValue);
  const user: Ref<User> = ref(new User());
  const Token: Ref<String> = ref(storedToken);
  const getUser = computed(() => user.value);
  const config = computed(() => {
    return { headers: { Authorization: `Bearer ${Token.value}` } };
  });

  async function Create(Credentials: Credentials) {
    await CreateUser(Credentials);
    await Login(Credentials);
  }

  async function Logout() {
    user.value = new User()
    loggedIn.value = false
    Token.value = ''
    localStorage.setItem('loggedIn', 'false')
    localStorage.setItem('token', '')
  }

  async function Login(Credentials: Credentials) {
    let token = await LoginUser(Credentials);
    if (typeof token === "string") {
      user.value.userName = Credentials.userName;
      Token.value = token;
      loggedIn.value = true;
      localStorage.setItem('loggedIn', 'true')
      localStorage.setItem('token', token)
    }
  }

  return { getUser, config, loggedIn, Login, Logout, Create };
});
